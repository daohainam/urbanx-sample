CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Categories" (
    "Id" uuid NOT NULL,
    "MerchantId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" text,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Categories" PRIMARY KEY ("Id")
);

CREATE TABLE "Merchants" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" text,
    "Email" character varying(100) NOT NULL,
    "Phone" text,
    "Address" text,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Merchants" PRIMARY KEY ("Id")
);

CREATE TABLE "Products" (
    "Id" uuid NOT NULL,
    "MerchantId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" text,
    "Price" numeric(18,2) NOT NULL,
    "StockQuantity" integer NOT NULL,
    "ImageUrl" text,
    "Category" text,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Products" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_Categories_MerchantId" ON "Categories" ("MerchantId");

CREATE UNIQUE INDEX "IX_Merchants_Email" ON "Merchants" ("Email");

CREATE INDEX "IX_Products_MerchantId" ON "Products" ("MerchantId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260209051009_InitialCreate', '10.0.1');

COMMIT;

