CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE campaigns (
    id uuid NOT NULL,
    name character varying(200) NOT NULL,
    subject character varying(500) NOT NULL,
    body text NOT NULL,
    sender_email character varying(320) NOT NULL,
    sender_name character varying(200) NOT NULL,
    status integer NOT NULL,
    total_recipients integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    started_at timestamp with time zone,
    completed_at timestamp with time zone,
    CONSTRAINT "PK_campaigns" PRIMARY KEY (id)
);

CREATE TABLE deliveries (
    id uuid NOT NULL,
    campaign_id uuid NOT NULL,
    recipient_email character varying(320) NOT NULL,
    recipient_name character varying(200),
    status integer NOT NULL,
    attempt_count integer NOT NULL,
    max_attempts integer NOT NULL,
    claim_version integer NOT NULL,
    error_message text,
    created_at timestamp with time zone NOT NULL,
    queued_at timestamp with time zone,
    sent_at timestamp with time zone,
    last_attempt_at timestamp with time zone,
    next_retry_at timestamp with time zone,
    lease_expires_at timestamp with time zone,
    updated_at timestamp with time zone NOT NULL,
    normalized_email character varying(320) GENERATED ALWAYS AS (lower(recipient_email)) STORED,
    CONSTRAINT "PK_deliveries" PRIMARY KEY (id),
    CONSTRAINT "FK_deliveries_campaigns_campaign_id" FOREIGN KEY (campaign_id) REFERENCES campaigns (id) ON DELETE RESTRICT
);

CREATE INDEX ix_deliveries_campaign_id_id_pending ON deliveries (campaign_id, id) WHERE status = 0;

CREATE UNIQUE INDEX ix_deliveries_campaign_id_normalized_email_unique ON deliveries (campaign_id, normalized_email);

CREATE INDEX ix_deliveries_campaign_id_status ON deliveries (campaign_id, status);

CREATE INDEX ix_deliveries_status_lease_expires_at ON deliveries (status, lease_expires_at) WHERE status = 2 AND lease_expires_at IS NOT NULL;

CREATE INDEX ix_deliveries_status_next_retry_at ON deliveries (status, next_retry_at) WHERE status = 3 AND next_retry_at IS NOT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261120001012_InitialCreate', '10.0.15');

COMMIT;

