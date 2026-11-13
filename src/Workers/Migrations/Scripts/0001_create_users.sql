CREATE TABLE IF NOT EXISTS users (
    id          uuid PRIMARY KEY,
    email       text NOT NULL,
    first_name  text NOT NULL,
    last_name   text NOT NULL,
    idp         text NULL,
    external_id text NOT NULL DEFAULT '',
    is_active   boolean NOT NULL DEFAULT true,
    created_at  timestamp NOT NULL,
    created_by  uuid NOT NULL,
    updated_at  timestamp NULL,
    updated_by  uuid NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_users_email ON users (LOWER(email));