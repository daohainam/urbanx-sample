CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Carts" (
    "Id" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Carts" PRIMARY KEY ("Id")
);

CREATE TABLE "Orders" (
    "Id" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "OrderNumber" text NOT NULL,
    "Status" integer NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "ShippingAddress" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Orders" PRIMARY KEY ("Id")
);

CREATE TABLE "CartItems" (
    "Id" uuid NOT NULL,
    "CartId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "ProductName" text NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    "MerchantId" uuid NOT NULL,
    CONSTRAINT "PK_CartItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CartItems_Carts_CartId" FOREIGN KEY ("CartId") REFERENCES "Carts" ("Id") ON DELETE CASCADE
);

CREATE TABLE "OrderItems" (
    "Id" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "ProductName" text NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    "MerchantId" uuid NOT NULL,
    CONSTRAINT "PK_OrderItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OrderItems_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE
);

CREATE TABLE "OrderStatusHistory" (
    "Id" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "Note" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_OrderStatusHistory" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OrderStatusHistory_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_CartItems_CartId" ON "CartItems" ("CartId");

CREATE INDEX "IX_Carts_CustomerId" ON "Carts" ("CustomerId");

CREATE INDEX "IX_OrderItems_OrderId" ON "OrderItems" ("OrderId");

CREATE INDEX "IX_Orders_CustomerId" ON "Orders" ("CustomerId");

CREATE UNIQUE INDEX "IX_Orders_OrderNumber" ON "Orders" ("OrderNumber");

CREATE INDEX "IX_OrderStatusHistory_OrderId_CreatedAt" ON "OrderStatusHistory" ("OrderId", "CreatedAt");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260209051034_InitialCreate', '10.0.1');

COMMIT;

