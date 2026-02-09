CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Payments" (
    "Id" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Status" integer NOT NULL,
    "Method" integer NOT NULL,
    "TransactionId" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Payments" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_Payments_OrderId" ON "Payments" ("OrderId");

CREATE INDEX "IX_Payments_TransactionId" ON "Payments" ("TransactionId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260209051021_InitialCreate', '10.0.1');

COMMIT;

