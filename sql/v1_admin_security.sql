-- Antihook v1.0: autorización y auditoría administrativa.
-- Aplicar después de respaldar la base de datos. No modifica tablas legacy.

CREATE TABLE IF NOT EXISTS antihook_roles (
    user_id INT UNSIGNED NOT NULL,
    role_name VARCHAR(32) NOT NULL DEFAULT 'player',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id),
    CONSTRAINT fk_antihook_role_user FOREIGN KEY (user_id)
        REFERENCES a_emu_playerinfo(user_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS antihook_bans (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    user_id INT UNSIGNED NULL,
    hwid_hash CHAR(64) NULL,
    ip_hash CHAR(64) NULL,
    reason VARCHAR(255) NOT NULL,
    created_by INT UNSIGNED NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NULL,
    active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    INDEX idx_antihook_bans_user (user_id),
    INDEX idx_antihook_bans_hwid (hwid_hash),
    INDEX idx_antihook_bans_active (active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS antihook_audit_events (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    actor_user_id INT UNSIGNED NULL,
    target_user_id INT UNSIGNED NULL,
    action_name VARCHAR(40) NOT NULL,
    reason VARCHAR(255) NOT NULL,
    metadata_json JSON NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_antihook_audit_created (created_at),
    INDEX idx_antihook_audit_target (target_user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Roles iniciales de ejemplo; sustituir IDs según el despliegue.
-- INSERT INTO antihook_roles (user_id, role_name) VALUES (1, 'admin')
-- ON DUPLICATE KEY UPDATE role_name = VALUES(role_name);
