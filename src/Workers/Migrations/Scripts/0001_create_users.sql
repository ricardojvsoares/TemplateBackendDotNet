CREATE TABLE IF NOT EXISTS users (
    id            uuid PRIMARY KEY,
    email         text NOT NULL,
    first_name    text NOT NULL,
    last_name     text NOT NULL,
    password_hash text NOT NULL,
    global_role   text NULL,
    permissions   text[] NOT NULL DEFAULT '{}',
    is_active     boolean NOT NULL DEFAULT true,
    created_at    timestamptz NOT NULL,
    created_by    uuid NOT NULL,
    updated_at    timestamptz NULL,
    updated_by    uuid NULL,
    deleted_at    timestamptz NULL,
    deleted_by    uuid NULL,
    CONSTRAINT ck_users_global_role
        CHECK (global_role IS NULL OR global_role IN ('viewer', 'maintainer', 'admin'))
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_email ON users (LOWER(email));

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id                uuid PRIMARY KEY,
    user_id           uuid NOT NULL REFERENCES users (id),
    token_hash        text NOT NULL,
    access_jti        text NOT NULL,
    access_expires_at timestamptz NOT NULL,
    expires_at        timestamptz NOT NULL,
    revoked_at        timestamptz NULL,
    created_at        timestamptz NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_refresh_tokens_token_hash ON refresh_tokens (token_hash);

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user_id ON refresh_tokens (user_id);
