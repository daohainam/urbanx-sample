# UrbanX - Nền Tảng Thương Mại Đa Nhà Bán

[View English version](README.md)

UrbanX là một nền tảng thương mại điện tử mẫu được xây dựng để minh họa cách một ứng dụng thực tế được tổ chức theo các kỹ thuật kỹ thuật phần mềm hiện đại. Dự án này được thiết kế phục vụ mục đích giáo dục, vì vậy mã nguồn được tổ chức để làm rõ từng khái niệm một cách dễ hiểu nhất.

Hệ thống được xây dựng dưới dạng tập hợp các **microservice** — những chương trình backend nhỏ, độc lập, mỗi chương trình xử lý một lĩnh vực nghiệp vụ riêng. Giao diện người dùng React đóng vai trò là giao diện dành cho khách hàng. Tất cả đều chạy sau một API Gateway đóng vai trò cửa vào duy nhất cho tất cả các yêu cầu.

---

## Dự Án Này Dạy Những Gì

Đây là một ví dụ thực hành về nhiều mẫu và công nghệ tiêu chuẩn trong ngành:

- **Kiến trúc microservices** — chia một ứng dụng lớn thành các dịch vụ nhỏ, có thể triển khai độc lập
- **Giao tiếp hướng sự kiện** — các dịch vụ giao tiếp với nhau bằng cách xuất bản và đăng ký các sự kiện thông qua Apache Kafka
- **CQRS (Phân tách trách nhiệm lệnh và truy vấn)** — phân tách thao tác ghi (PostgreSQL) khỏi thao tác đọc (Elasticsearch) để có hiệu suất tốt hơn
- **Mẫu Saga (dựa trên choreography)** — phối hợp một quy trình nghiệp vụ nhiều bước (đặt hàng → đặt chỗ hàng tồn kho → xử lý thanh toán → thông báo nhà bán) mà không cần bộ điều phối trung tâm
- **Transactional outbox** — kỹ thuật đảm bảo tin nhắn không bao giờ bị mất ngay cả khi dịch vụ gặp sự cố sau khi lưu dữ liệu nhưng trước khi gửi tin nhắn
- **Xác thực JWT và phân quyền dựa trên chính sách** — bảo mật API để chỉ đúng người dùng mới có thể truy cập đúng endpoint
- **API Gateway với giới hạn tốc độ** — một cửa vào duy nhất bảo vệ các dịch vụ backend khỏi bị quá tải
- **OpenTelemetry và distributed tracing** — quan sát những gì xảy ra trên nhiều dịch vụ khi một yêu cầu đơn lẻ được xử lý

---

## Tổng Quan Hệ Thống

Khi khách hàng đặt hàng, chuỗi sự kiện sau đây xảy ra tự động trên nhiều dịch vụ:

1. Khách hàng thêm sản phẩm vào giỏ hàng (Order Service) và thanh toán.
2. Order Service tạo đơn hàng và xuất bản sự kiện `OrderCreated` lên Kafka.
3. Inventory Service nhận sự kiện, kiểm tra hàng tồn kho, đặt chỗ các mặt hàng và xuất bản sự kiện `InventoryReserved` hoặc `InventoryFailed`.
4. Nếu hàng tồn kho được đặt chỗ, Payment Service xử lý thanh toán qua Stripe và xuất bản sự kiện `PaymentCompleted` hoặc `PaymentFailed`.
5. Nếu thanh toán thành công, Merchant Service được thông báo để nhà bán có thể chấp nhận và thực hiện đơn hàng.
6. Nếu bất kỳ bước nào thất bại, đơn hàng sẽ bị hủy và khách hàng được thông báo.

Luồng này là một ví dụ về mẫu **Saga (choreography)** — không có dịch vụ trung tâm nào kiểm soát toàn bộ quy trình; mỗi dịch vụ phản ứng với các sự kiện từ bước trước.

---

## Kiến Trúc

### Các Microservice

| Dịch vụ | Cổng | Mô tả |
|---------|------|-------|
| Catalog Service | 5001 | Quản lý danh mục sản phẩm. Sử dụng CQRS: ghi vào PostgreSQL, đọc từ Elasticsearch để tìm kiếm nhanh. |
| Order Service | 5002 | Xử lý giỏ hàng, tạo đơn hàng và theo dõi trạng thái đơn hàng. |
| Merchant Service | 5003 | Quản lý tài khoản nhà bán và danh sách sản phẩm của họ. |
| Payment Service | 5004 | Xử lý thanh toán sử dụng Stripe. Dùng transactional outbox để xuất bản sự kiện thanh toán một cách đáng tin cậy. |
| Inventory Service | (động) | Theo dõi mức tồn kho. Đặt chỗ hàng khi đặt hàng và giải phóng nếu đơn hàng bị hủy. |
| Identity Service | 5005 | Xử lý đăng ký người dùng, đăng nhập và phát hành token JWT bằng Duende IdentityServer. |
| API Gateway | 5000 | Cửa vào duy nhất cho tất cả các yêu cầu từ client. Định tuyến lưu lượng đến đúng dịch vụ bằng YARP. Thực thi giới hạn tốc độ. |

### Frontend

Frontend là một ứng dụng trang đơn (SPA) React 19 nằm trong `src/Frontend/urbanx-react`. Nó kết nối với backend hoàn toàn thông qua API Gateway và xác thực người dùng qua OpenID Connect (OIDC).

### Cơ Sở Hạ Tầng

| Thành phần | Mục đích |
|-----------|---------|
| PostgreSQL | Mỗi dịch vụ có cơ sở dữ liệu riêng (mẫu database-per-service). |
| Apache Kafka | Message broker được sử dụng để giao tiếp hướng sự kiện bất đồng bộ giữa các dịch vụ. |
| Elasticsearch | Lưu trữ bản sao có thể tìm kiếm của danh mục sản phẩm để tìm kiếm toàn văn nhanh. |
| .NET Aspire | Công cụ thời gian phát triển khởi động tất cả các dịch vụ cùng nhau, cung cấp service discovery, kiểm tra sức khỏe và dashboard quan sát trực tiếp. |

---

## Giải Thích Các Mẫu Kiến Trúc

### Transactional Outbox

**Vấn đề nó giải quyết:** Một dịch vụ lưu dữ liệu vào cơ sở dữ liệu và sau đó cần gửi một tin nhắn (sự kiện) lên Kafka. Nếu dịch vụ gặp sự cố giữa hai bước này, dữ liệu được lưu nhưng tin nhắn không bao giờ được gửi. Các dịch vụ khác không bao giờ biết điều gì đã xảy ra.

**Cách hoạt động:** Thay vì xuất bản lên Kafka trực tiếp, dịch vụ lưu sự kiện dưới dạng một hàng trong bảng "outbox" trong *cùng một giao dịch cơ sở dữ liệu* với dữ liệu nghiệp vụ. Một worker nền (`OutboxRelayService`) sau đó đọc từ bảng outbox này và xuất bản các tin nhắn lên Kafka. Điều này đảm bảo rằng nếu dữ liệu được lưu, tin nhắn cuối cùng sẽ được gửi — ngay cả sau khi gặp sự cố.

Các dịch vụ sử dụng mẫu này: Catalog, Order, Payment, Inventory.

### Saga (Choreography)

**Vấn đề nó giải quyết:** Hoàn thành một đơn hàng yêu cầu nhiều bước trên nhiều dịch vụ. Nếu một bước thất bại, tất cả các bước trước đó phải được hoàn tác. Làm thế nào để phối hợp điều này mà không kết hợp chặt chẽ các dịch vụ lại với nhau?

**Cách hoạt động (kiểu choreography):** Không có bộ điều phối trung tâm. Mỗi dịch vụ lắng nghe sự kiện và phản ứng với chúng. Ví dụ:
- Order Service xuất bản `OrderCreated`.
- Inventory Service nghe thấy điều này và đặt chỗ hàng, sau đó xuất bản `InventoryReserved`.
- Payment Service nghe thấy điều này và tính phí khách hàng, sau đó xuất bản `PaymentCompleted`.
- Merchant Service nghe thấy điều này và đánh dấu đơn hàng sẵn sàng để thực hiện.

Nếu có gì đó xảy ra sai, một sự kiện thất bại kích hoạt các bước bù đắp phù hợp (hủy đơn hàng, giải phóng hàng đặt chỗ, v.v.).

### CQRS (Phân Tách Trách Nhiệm Lệnh và Truy Vấn)

**Vấn đề nó giải quyết:** Cơ sở dữ liệu sản phẩm được tối ưu hóa cho các lần ghi an toàn, có giao dịch (PostgreSQL) không phải lúc nào cũng là công cụ tốt nhất cho các truy vấn tìm kiếm nhanh, linh hoạt.

**Cách hoạt động:** Catalog Service ghi tất cả các thay đổi sản phẩm vào PostgreSQL (phía "lệnh"). Đồng thời, nó xuất bản các sự kiện cập nhật chỉ mục Elasticsearch (phía "truy vấn"). Các yêu cầu đọc — như tìm kiếm sản phẩm — đi thẳng đến Elasticsearch để tăng tốc độ, trong khi các lần ghi luôn đi qua PostgreSQL để đảm bảo tính nhất quán.

---

## Bắt Đầu

### Điều Kiện Tiên Quyết

Trước khi chạy dự án, hãy đảm bảo bạn đã cài đặt những thứ sau:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker và Docker Compose](https://www.docker.com/)

### Tùy Chọn 1: .NET Aspire (Khuyến nghị cho phát triển)

.NET Aspire là cách dễ nhất để chạy toàn bộ dự án cục bộ. Nó tự động khởi động PostgreSQL và Kafka trong Docker, kết nối tất cả các dịch vụ lại với nhau và mở một dashboard nơi bạn có thể xem log và trace từ mọi dịch vụ ở một nơi.

**Bước 1:** Cài đặt workload Aspire (chỉ cần một lần):

```bash
dotnet workload install aspire
```

**Bước 2:** Khởi động tất cả các dịch vụ backend:

```bash
cd src/AppHost/UrbanX.AppHost
dotnet run
```

Aspire Dashboard sẽ mở tại `http://localhost:15260`. Hãy đợi cho đến khi tất cả các dịch vụ hiển thị trạng thái healthy trước khi tiếp tục.

**Bước 3:** Khởi động frontend trong terminal mới:

```bash
cd src/Frontend/urbanx-react
npm install
npm run dev
```

Ứng dụng hiện có tại:

- Frontend: `http://localhost:5173`
- API Gateway: Hiển thị trong Aspire Dashboard (được gán động)
- Aspire Dashboard: `http://localhost:15260`

### Tùy Chọn 2: Cài Đặt Thủ Công

Sử dụng cách này nếu bạn muốn khởi động từng thành phần riêng lẻ hoặc nếu bạn không sử dụng Aspire.

**Bước 1:** Khởi động cơ sở hạ tầng (PostgreSQL và Kafka) bằng Docker:

```bash
docker-compose up -d
```

Lệnh này khởi động:
- PostgreSQL trên cổng 5432
- Kafka trên cổng 9092
- Zookeeper trên cổng 2181

**Bước 2:** Sao chép tệp môi trường và điền vào các giá trị cần thiết:

```bash
cp .env.example .env
```

**Bước 3:** Khởi động từng dịch vụ backend trong terminal riêng của nó. Identity Service phải khởi động trước vì các dịch vụ khác phụ thuộc vào nó để xác thực:

```bash
# Terminal 1 - Identity Service (khởi động cái này trước)
cd src/Services/Identity/UrbanX.Services.Identity && dotnet run

# Terminal 2 - Catalog Service
cd src/Services/Catalog/UrbanX.Services.Catalog && dotnet run

# Terminal 3 - Order Service
cd src/Services/Order/UrbanX.Services.Order && dotnet run

# Terminal 4 - Merchant Service
cd src/Services/Merchant/UrbanX.Services.Merchant && dotnet run

# Terminal 5 - Payment Service
cd src/Services/Payment/UrbanX.Services.Payment && dotnet run

# Terminal 6 - Inventory Service
cd src/Services/Inventory/UrbanX.Services.Inventory && dotnet run

# Terminal 7 - API Gateway
cd src/Gateway/UrbanX.Gateway && dotnet run
```

Ngoài ra, hãy dùng các script được cung cấp:

```bash
# Linux / macOS
./start-services.sh

# Windows PowerShell
.\start-services.ps1
```

**Bước 4:** Khởi động frontend:

```bash
cd src/Frontend/urbanx-react
npm install
npm run dev
```

Ứng dụng hiện có tại:

- Frontend: `http://localhost:5173`
- API Gateway: `http://localhost:5000`
- Identity Service: `http://localhost:5005`

---

## Các Endpoint API

Tất cả các yêu cầu API từ frontend đều đi qua API Gateway tại `http://localhost:5000`. Gateway chuyển tiếp chúng đến dịch vụ phù hợp.

Các endpoint được đánh dấu **[Yêu cầu xác thực]** cần có JWT bearer token hợp lệ trong header yêu cầu `Authorization: Bearer <token>`. Token được lấy bằng cách đăng nhập thông qua Identity Service.

### Catalog Service — `/api/products`

| Phương thức | Đường dẫn | Quyền truy cập | Mô tả |
|--------|------|--------|-------------|
| GET | `/api/products` | Công khai | Tìm kiếm và liệt kê tất cả sản phẩm |
| GET | `/api/products/{id}` | Công khai | Lấy chi tiết của một sản phẩm cụ thể |
| GET | `/api/products/merchant/{merchantId}` | Công khai | Liệt kê sản phẩm cho một nhà bán cụ thể |
| POST | `/api/products` | [Yêu cầu xác thực] Nhà bán | Tạo sản phẩm mới |
| PUT | `/api/products/{id}` | [Yêu cầu xác thực] Nhà bán | Cập nhật sản phẩm hiện có |
| DELETE | `/api/products/{id}` | [Yêu cầu xác thực] Nhà bán | Xóa sản phẩm |

### Order Service — `/api/cart` và `/api/orders`

| Phương thức | Đường dẫn | Quyền truy cập | Mô tả |
|--------|------|--------|-------------|
| GET | `/api/cart/{customerId}` | [Yêu cầu xác thực] Khách hàng | Lấy giỏ hàng hiện tại của khách hàng |
| POST | `/api/cart/{customerId}/items` | [Yêu cầu xác thực] Khách hàng | Thêm mặt hàng vào giỏ hàng |
| DELETE | `/api/cart/{customerId}/items/{itemId}` | [Yêu cầu xác thực] Khách hàng | Xóa mặt hàng khỏi giỏ hàng |
| POST | `/api/orders` | [Yêu cầu xác thực] Khách hàng | Đặt hàng (thanh toán) |
| GET | `/api/orders/{orderId}` | [Yêu cầu xác thực] Khách hàng hoặc Nhà bán | Lấy chi tiết của một đơn hàng cụ thể |
| GET | `/api/orders/customer/{customerId}` | [Yêu cầu xác thực] Khách hàng | Liệt kê tất cả đơn hàng của một khách hàng |
| PUT | `/api/orders/{orderId}/status` | [Yêu cầu xác thực] Nhà bán | Cập nhật trạng thái của một đơn hàng |

### Merchant Service — `/api/merchants`

| Phương thức | Đường dẫn | Quyền truy cập | Mô tả |
|--------|------|--------|-------------|
| GET | `/api/merchants/{id}` | Công khai | Lấy hồ sơ nhà bán |
| POST | `/api/merchants` | [Yêu cầu xác thực] Nhà bán | Đăng ký làm nhà bán mới |
| GET | `/api/merchants/{merchantId}/products` | Công khai | Liệt kê sản phẩm của một nhà bán |
| POST | `/api/merchants/{merchantId}/products` | [Yêu cầu xác thực] Nhà bán | Thêm sản phẩm |
| PUT | `/api/merchants/{merchantId}/products/{productId}` | [Yêu cầu xác thực] Nhà bán | Cập nhật sản phẩm |
| DELETE | `/api/merchants/{merchantId}/products/{productId}` | [Yêu cầu xác thực] Nhà bán | Xóa sản phẩm |

### Payment Service — `/api/payments`

| Phương thức | Đường dẫn | Quyền truy cập | Mô tả |
|--------|------|--------|-------------|
| POST | `/api/payments` | [Yêu cầu xác thực] Khách hàng | Gửi thanh toán cho một đơn hàng |
| GET | `/api/payments/{id}` | [Yêu cầu xác thực] Khách hàng hoặc Nhà bán | Lấy bản ghi thanh toán theo ID |
| GET | `/api/payments/order/{orderId}` | [Yêu cầu xác thực] Khách hàng hoặc Nhà bán | Lấy thanh toán cho một đơn hàng cụ thể |

### Inventory Service — `/api/inventory`

| Phương thức | Đường dẫn | Quyền truy cập | Mô tả |
|--------|------|--------|-------------|
| GET | `/api/inventory/{productId}` | [Yêu cầu xác thực] Nhà bán | Lấy bản ghi tồn kho cho một sản phẩm |
| POST | `/api/inventory` | [Yêu cầu xác thực] Nhà bán | Tạo bản ghi tồn kho cho sản phẩm mới |
| PUT | `/api/inventory/{productId}` | [Yêu cầu xác thực] Nhà bán | Cập nhật số lượng tồn kho cho một sản phẩm |
| GET | `/api/inventory/reservations/{orderId}` | [Yêu cầu xác thực] Khách hàng hoặc Nhà bán | Lấy các đặt chỗ tồn kho cho một đơn hàng |

---

## Công Nghệ Sử Dụng

### Backend

| Công nghệ | Mục đích |
|------------|---------|
| .NET 10 / ASP.NET Core | Framework chính cho tất cả các dịch vụ backend, sử dụng kiểu Minimal API |
| .NET Aspire | Điều phối phát triển, service discovery, kiểm tra sức khỏe và dashboard distributed tracing |
| Entity Framework Core | Bộ ánh xạ quan hệ đối tượng (ORM) để truy cập cơ sở dữ liệu |
| PostgreSQL | Cơ sở dữ liệu quan hệ; mỗi dịch vụ có cơ sở dữ liệu cô lập riêng |
| Apache Kafka | Message broker phân tán cho giao tiếp hướng sự kiện |
| Elasticsearch | Công cụ tìm kiếm toàn văn được sử dụng cho read model danh mục sản phẩm |
| Duende IdentityServer | Server OpenID Connect và OAuth 2.0 để phát hành token JWT |
| YARP | Thư viện "Yet Another Reverse Proxy" được sử dụng để xây dựng API Gateway |
| Stripe SDK | Tích hợp xử lý thanh toán |
| OpenTelemetry | Thu thập distributed tracing và metrics |

### Frontend

| Công nghệ | Mục đích |
|------------|---------|
| React 19 + TypeScript | Thư viện UI và ngôn ngữ để xây dựng ứng dụng dành cho khách hàng |
| Vite | Server phát triển nhanh và công cụ build |
| Tailwind CSS 4 | Framework CSS utility-first để tạo kiểu |
| React Router | Định tuyến phía client |
| oidc-client-ts | Xử lý luồng đăng nhập OpenID Connect với Identity Service |

### Thư Viện Dùng Chung

| Thư viện | Mục đích |
|---------|---------|
| `UrbanX.Shared.Security` | Tiện ích bảo mật có thể tái sử dụng: chính sách phân quyền JWT, helper xác thực đầu vào và global exception handler |
| `UrbanX.ServiceDefaults` | Cấu hình Aspire chung được áp dụng cho tất cả các dịch vụ: kiểm tra sức khỏe, telemetry và chính sách resilience |

---

## Cấu Trúc Dự Án

```
urbanx-sample/
├── src/
│   ├── AppHost/
│   │   └── UrbanX.AppHost/           # .NET Aspire host — khởi động và kết nối tất cả dịch vụ
│   ├── ServiceDefaults/
│   │   └── UrbanX.ServiceDefaults/   # Mặc định Aspire dùng chung (health, telemetry, resilience)
│   ├── Services/
│   │   ├── Catalog/                  # Dịch vụ danh mục sản phẩm (CQRS, Elasticsearch)
│   │   ├── Order/                    # Dịch vụ giỏ hàng và đơn hàng (điều phối Saga qua sự kiện)
│   │   ├── Merchant/                 # Đăng ký và quản lý nhà bán
│   │   ├── Payment/                  # Xử lý thanh toán qua Stripe
│   │   ├── Inventory/                # Theo dõi và đặt chỗ tồn kho
│   │   └── Identity/                 # Server xác thực (Duende IdentityServer)
│   ├── Gateway/
│   │   └── UrbanX.Gateway/           # API Gateway (YARP reverse proxy + giới hạn tốc độ)
│   ├── Frontend/
│   │   └── urbanx-react/             # React SPA dành cho khách hàng
│   └── Shared/
│       ├── UrbanX.Shared/            # Domain model và hợp đồng dùng chung
│       └── UrbanX.Shared.Security/   # Helper bảo mật dùng trên tất cả dịch vụ
├── tests/                            # Unit test và integration test cho tất cả dịch vụ
├── kubernetes/                       # Manifest triển khai Kubernetes
├── docker-compose.yml                # Cơ sở hạ tầng cục bộ (PostgreSQL, Kafka, Elasticsearch)
├── docker-compose.production.yml     # Cấu hình Docker Compose cho production
├── generate-migrations.sh            # Script helper để tạo migration EF Core
├── start-services.sh                 # Script Linux/macOS để khởi động tất cả dịch vụ
├── start-services.ps1                # Script Windows PowerShell để khởi động tất cả dịch vụ
└── UrbanX.sln                        # Tệp solution .NET
```

---

## Hướng Dẫn Phát Triển

### Chạy Tests

Thư mục `tests/` chứa cả unit test và integration test cho mỗi dịch vụ. Để chạy tất cả test:

```bash
dotnet test UrbanX.sln
```

### Hot Reload

- **Frontend:** Hot reload được bật tự động khi bạn chạy `npm run dev`. Các thay đổi đối với component React xuất hiện ngay lập tức trong trình duyệt.
- **Backend:** Dùng `dotnet watch run` thay vì `dotnet run` để bật hot reload cho các dịch vụ .NET.

### Database Migrations

Các migration schema cơ sở dữ liệu được áp dụng tự động khi mỗi dịch vụ khởi động. Để tạo migration mới sau khi thay đổi data model:

```bash
cd src/Services/<TênDịchVụ>/UrbanX.Services.<TênDịchVụ>
dotnet ef migrations add <TênMigration> --context <TênDịchVụ>DbContext
```

Thay `<TênDịchVụ>` bằng tên của dịch vụ bạn đã thay đổi (ví dụ: `Catalog`, `Order`, `Merchant`).

Xem [DATABASE_MIGRATIONS.md](DATABASE_MIGRATIONS.md) để được hướng dẫn đầy đủ.

### Cấu Hình Môi Trường

Các giá trị nhạy cảm như mật khẩu cơ sở dữ liệu và khóa Stripe API được quản lý thông qua biến môi trường. Một template được cung cấp:

```bash
cp .env.example .env
```

Mở `.env` trong trình soạn thảo văn bản và điền vào các giá trị cần thiết trước khi khởi động các dịch vụ thủ công.

### Kiểm Tra API Endpoint Thủ Công

Mỗi dịch vụ bao gồm tệp `.http` mà bạn có thể dùng để kiểm tra endpoint trực tiếp từ Visual Studio Code (với extension REST Client) hoặc JetBrains Rider:

- `UrbanX.Services.Catalog.http`
- `UrbanX.Services.Order.http`
- `UrbanX.Services.Merchant.http`
- `UrbanX.Services.Payment.http`

Bạn cũng có thể dùng `curl` để kiểm tra API thông qua gateway:

```bash
# Liệt kê tất cả sản phẩm (không cần xác thực)
curl http://localhost:5000/api/products

# Lấy sản phẩm cụ thể theo ID
curl http://localhost:5000/api/products/<product-id>
```

### Xử Lý Sự Cố

**Cổng đã được sử dụng:** Thay đổi cổng trong tệp `Properties/launchSettings.json` của dịch vụ.

**Sự cố kết nối cơ sở dữ liệu:**
```bash
docker-compose ps               # Kiểm tra container nào đang chạy
docker-compose up -d postgres   # Khởi động lại container PostgreSQL
```

**Frontend không tải được:**
```bash
cd src/Frontend/urbanx-react
rm -rf node_modules package-lock.json
npm install
```

**Lỗi build backend:**
```bash
dotnet clean && dotnet build
```

---

## Bảo Mật

- **Xác thực JWT** — Tất cả các endpoint nhạy cảm yêu cầu JWT bearer token hợp lệ do Identity Service phát hành.
- **Phân quyền dựa trên chính sách** — Ba chính sách phân quyền được định nghĩa: `CustomerOnly`, `MerchantOnly` và `CustomerOrMerchant`. Mỗi endpoint khai báo chính sách nào áp dụng.
- **Giới hạn tốc độ** — API Gateway giới hạn mỗi địa chỉ IP ở 100 yêu cầu mỗi phút. Các yêu cầu vượt quá giới hạn này nhận được phản hồi HTTP 429 với header `Retry-After`.
- **Xác thực đầu vào** — Tất cả các endpoint ghi xác thực dữ liệu yêu cầu bằng các helper trong `UrbanX.Shared.Security.RequestValidation`.
- **Global exception handling** — Các ngoại lệ chưa được xử lý được bắt và trả về dưới dạng phản hồi lỗi được chuẩn hóa. Stack trace bị ẩn trong môi trường production.
- **Security headers** — Tất cả các phản hồi bao gồm các header `X-Content-Type-Options`, `X-Frame-Options`, `X-XSS-Protection` và `Referrer-Policy`.

Xem [SECURITY.md](SECURITY.md) để được hướng dẫn bảo mật production.

---

## Triển Khai

### Docker Compose (Production)

Tệp Docker Compose production-ready được bao gồm:

```bash
docker-compose -f docker-compose.production.yml up -d
```

### Kubernetes

Các manifest Kubernetes cho tất cả dịch vụ nằm trong thư mục `kubernetes/`:

```bash
kubectl apply -f kubernetes/
```

Xem [PRODUCTION_DEPLOYMENT.md](PRODUCTION_DEPLOYMENT.md) để có hướng dẫn triển khai từng bước đầy đủ.

---

## Giấy Phép

MIT
