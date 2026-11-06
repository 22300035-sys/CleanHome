CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261018193455_InitialServiceCatalog') THEN
    CREATE TABLE service_categories (
        id uuid NOT NULL,
        name character varying(120) NOT NULL,
        slug character varying(120) NOT NULL,
        description character varying(1000) NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_service_categories" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261018193455_InitialServiceCatalog') THEN
    CREATE INDEX "IX_service_categories_is_active" ON service_categories (is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261018193455_InitialServiceCatalog') THEN
    CREATE UNIQUE INDEX "IX_service_categories_slug" ON service_categories (slug);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261018193455_InitialServiceCatalog') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261018193455_InitialServiceCatalog', '10.0.14');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261018193942_SeedServiceCategories') THEN
    INSERT INTO service_categories (id, name, slug, description, is_active, created_at, updated_at)
    VALUES ('a1111111-1111-4111-8111-111111111111', 'Limpieza general', 'limpieza-general', 'Limpieza habitual de viviendas, incluyendo pisos, superficies, cocina y baños.', TRUE, TIMESTAMPTZ '2026-10-18T00:00:00Z', TIMESTAMPTZ '2026-10-18T00:00:00Z');
    INSERT INTO service_categories (id, name, slug, description, is_active, created_at, updated_at)
    VALUES ('a2222222-2222-4222-8222-222222222222', 'Limpieza profunda', 'limpieza-profunda', 'Limpieza detallada de viviendas, incluyendo áreas que requieren mayor tiempo y atención.', TRUE, TIMESTAMPTZ '2026-10-18T00:00:00Z', TIMESTAMPTZ '2026-10-18T00:00:00Z');
    INSERT INTO service_categories (id, name, slug, description, is_active, created_at, updated_at)
    VALUES ('a3333333-3333-4333-8333-333333333333', 'Limpieza de oficinas pequeñas', 'limpieza-oficinas', 'Limpieza ordinaria de oficinas pequeñas y espacios de trabajo, excluyendo actividades industriales o de alto riesgo.', TRUE, TIMESTAMPTZ '2026-10-18T00:00:00Z', TIMESTAMPTZ '2026-10-18T00:00:00Z');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261018193942_SeedServiceCategories') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261018193942_SeedServiceCategories', '10.0.14');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE "AspNetRoles" (
        "Id" text NOT NULL,
        "Name" character varying(256),
        "NormalizedName" character varying(256),
        "ConcurrencyStamp" text,
        CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE "AspNetUsers" (
        "Id" text NOT NULL,
        "UserName" character varying(256),
        "NormalizedUserName" character varying(256),
        "Email" character varying(256),
        "NormalizedEmail" character varying(256),
        "EmailConfirmed" boolean NOT NULL,
        "PasswordHash" text,
        "SecurityStamp" text,
        "ConcurrencyStamp" text,
        "PhoneNumber" text,
        "PhoneNumberConfirmed" boolean NOT NULL,
        "TwoFactorEnabled" boolean NOT NULL,
        "LockoutEnd" timestamp with time zone,
        "LockoutEnabled" boolean NOT NULL,
        "AccessFailedCount" integer NOT NULL,
        CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE audit_entries (
        "Id" uuid NOT NULL,
        "ActorUserId" character varying(450) NOT NULL,
        "Action" character varying(100) NOT NULL,
        "ResourceId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_audit_entries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE "AspNetRoleClaims" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "RoleId" text NOT NULL,
        "ClaimType" text,
        "ClaimValue" text,
        CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE "AspNetUserClaims" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "UserId" text NOT NULL,
        "ClaimType" text,
        "ClaimValue" text,
        CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE "AspNetUserLogins" (
        "LoginProvider" text NOT NULL,
        "ProviderKey" text NOT NULL,
        "ProviderDisplayName" text,
        "UserId" text NOT NULL,
        CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
        CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE "AspNetUserRoles" (
        "UserId" text NOT NULL,
        "RoleId" text NOT NULL,
        CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
        CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE "AspNetUserTokens" (
        "UserId" text NOT NULL,
        "LoginProvider" text NOT NULL,
        "Name" text NOT NULL,
        "Value" text,
        CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
        CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE user_profiles (
        "Id" uuid NOT NULL,
        "UserId" character varying(450) NOT NULL,
        "DisplayName" character varying(120) NOT NULL,
        "Role" integer NOT NULL,
        "IsVerified" boolean NOT NULL,
        "IsSuspended" boolean NOT NULL,
        "Version" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_profiles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_user_profiles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE customer_locations (
        "Id" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "Label" character varying(80) NOT NULL,
        "Address" character varying(500) NOT NULL,
        "City" character varying(120) NOT NULL,
        CONSTRAINT "PK_customer_locations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_customer_locations_user_profiles_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES user_profiles ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE notifications (
        "Id" uuid NOT NULL,
        "RecipientId" uuid NOT NULL,
        "Kind" character varying(100) NOT NULL,
        "ResourceId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ReadAt" timestamp with time zone,
        CONSTRAINT "PK_notifications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_notifications_user_profiles_RecipientId" FOREIGN KEY ("RecipientId") REFERENCES user_profiles ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE private_documents (
        "Id" uuid NOT NULL,
        "OwnerId" uuid NOT NULL,
        "StorageKey" character varying(64) NOT NULL,
        "OriginalName" character varying(200) NOT NULL,
        "ContentType" character varying(100) NOT NULL,
        "Length" bigint NOT NULL,
        "Status" integer NOT NULL,
        "ReviewNotes" character varying(1000) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_private_documents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_private_documents_user_profiles_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES user_profiles ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE service_requests (
        "Id" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "LocationId" uuid NOT NULL,
        "CategoryId" uuid NOT NULL,
        "Description" character varying(2000) NOT NULL,
        "StartsAt" timestamp with time zone NOT NULL,
        "DurationMinutes" integer NOT NULL,
        "Status" integer NOT NULL,
        "Version" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_service_requests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_service_requests_customer_locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES customer_locations ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_service_requests_service_categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES service_categories (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_service_requests_user_profiles_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES user_profiles ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE service_quotes (
        "Id" uuid NOT NULL,
        "RequestId" uuid NOT NULL,
        "WorkerId" uuid NOT NULL,
        "Amount" numeric(12,2) NOT NULL,
        "Currency" character varying(3) NOT NULL,
        "Notes" character varying(1000) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_service_quotes" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_quote_amount" CHECK ("Amount" > 0),
        CONSTRAINT "FK_service_quotes_service_requests_RequestId" FOREIGN KEY ("RequestId") REFERENCES service_requests ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_service_quotes_user_profiles_WorkerId" FOREIGN KEY ("WorkerId") REFERENCES user_profiles ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE service_bookings (
        "Id" uuid NOT NULL,
        "RequestId" uuid NOT NULL,
        "QuoteId" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "WorkerId" uuid NOT NULL,
        "StartsAt" timestamp with time zone NOT NULL,
        "EndsAt" timestamp with time zone NOT NULL,
        "Amount" numeric(12,2) NOT NULL,
        "Currency" character varying(3) NOT NULL,
        "Status" integer NOT NULL,
        "Version" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_service_bookings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_service_bookings_service_quotes_QuoteId" FOREIGN KEY ("QuoteId") REFERENCES service_quotes ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_service_bookings_service_requests_RequestId" FOREIGN KEY ("RequestId") REFERENCES service_requests ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_service_bookings_user_profiles_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES user_profiles ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_service_bookings_user_profiles_WorkerId" FOREIGN KEY ("WorkerId") REFERENCES user_profiles ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE chat_messages (
        "Id" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "SenderId" uuid NOT NULL,
        "Text" character varying(2000) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_chat_messages" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_chat_messages_service_bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES service_bookings ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_chat_messages_user_profiles_SenderId" FOREIGN KEY ("SenderId") REFERENCES user_profiles ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE TABLE payments (
        "Id" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "Provider" character varying(20) NOT NULL,
        "CheckoutId" character varying(200),
        "ProviderPaymentId" character varying(200),
        "CheckoutUrl" character varying(2048),
        "Amount" numeric(12,2) NOT NULL,
        "Currency" character varying(3) NOT NULL,
        "Status" integer NOT NULL,
        "Version" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_payments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_payments_service_bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES service_bookings ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_audit_entries_CreatedAt" ON audit_entries ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_chat_messages_BookingId_CreatedAt_Id" ON chat_messages ("BookingId", "CreatedAt", "Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_chat_messages_SenderId" ON chat_messages ("SenderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_customer_locations_CustomerId" ON customer_locations ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_notifications_RecipientId_CreatedAt" ON notifications ("RecipientId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE UNIQUE INDEX "IX_payments_BookingId" ON payments ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_private_documents_OwnerId" ON private_documents ("OwnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_service_bookings_CustomerId" ON service_bookings ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE UNIQUE INDEX "IX_service_bookings_QuoteId" ON service_bookings ("QuoteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE UNIQUE INDEX "IX_service_bookings_RequestId" ON service_bookings ("RequestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_service_bookings_WorkerId_StartsAt_EndsAt" ON service_bookings ("WorkerId", "StartsAt", "EndsAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE UNIQUE INDEX "IX_service_quotes_RequestId_WorkerId" ON service_quotes ("RequestId", "WorkerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_service_quotes_WorkerId" ON service_quotes ("WorkerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_service_requests_CategoryId" ON service_requests ("CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_service_requests_CustomerId" ON service_requests ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_service_requests_LocationId" ON service_requests ("LocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE INDEX "IX_service_requests_Status_StartsAt" ON service_requests ("Status", "StartsAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    CREATE UNIQUE INDEX "IX_user_profiles_UserId" ON user_profiles ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104024732_MarketplaceAndIdentity') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261104024732_MarketplaceAndIdentity', '10.0.14');
    END IF;
END $EF$;
COMMIT;

