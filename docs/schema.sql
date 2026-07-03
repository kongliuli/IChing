-- IChing MVP core schema (Flyway V1__init.sql mirrors this)

CREATE TABLE IF NOT EXISTS t_user (
  user_id           BIGINT       NOT NULL AUTO_INCREMENT,
  username          VARCHAR(50)  DEFAULT NULL,
  phone             VARCHAR(20)  DEFAULT NULL,
  password          VARCHAR(128) DEFAULT NULL,
  nickname          VARCHAR(50)  DEFAULT NULL,
  avatar            VARCHAR(255) DEFAULT NULL,
  birthday          DATE         DEFAULT NULL,
  birth_hour        TINYINT      DEFAULT NULL COMMENT '0-23',
  gender            TINYINT      DEFAULT NULL COMMENT '0未知 1男 2女',
  province          VARCHAR(50)  DEFAULT NULL,
  city              VARCHAR(50)  DEFAULT NULL,
  birth_data_enc    TEXT         DEFAULT NULL COMMENT 'AES encrypted birth JSON',
  member_level      TINYINT      NOT NULL DEFAULT 0 COMMENT '0普通 1VIP',
  member_expire_time DATETIME    DEFAULT NULL,
  role              VARCHAR(20)  NOT NULL DEFAULT 'USER' COMMENT 'USER or ADMIN',
  status            TINYINT      NOT NULL DEFAULT 1 COMMENT '0禁用 1正常',
  create_time       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  update_time       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (user_id),
  UNIQUE KEY uk_phone (phone)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户表';

CREATE TABLE IF NOT EXISTS t_paipan (
  paipan_id    BIGINT       NOT NULL AUTO_INCREMENT,
  user_id      BIGINT       NOT NULL,
  type         TINYINT      NOT NULL DEFAULT 1 COMMENT '1八字',
  title        VARCHAR(100) DEFAULT NULL,
  birth_data   JSON         NOT NULL,
  result_data  JSON         NOT NULL,
  is_paid      TINYINT      NOT NULL DEFAULT 0,
  report_id    BIGINT       DEFAULT NULL,
  create_time  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (paipan_id),
  KEY idx_paipan_user (user_id),
  KEY idx_paipan_time (create_time)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='排盘记录';

CREATE TABLE IF NOT EXISTS t_paipan_report (
  report_id          BIGINT   NOT NULL AUTO_INCREMENT,
  paipan_id          BIGINT   NOT NULL,
  order_id           BIGINT   DEFAULT NULL,
  user_id            BIGINT   NOT NULL,
  content            LONGTEXT DEFAULT NULL,
  ai_interpretation  TEXT     DEFAULT NULL,
  status             TINYINT  NOT NULL DEFAULT 0 COMMENT '0生成中 1已完成',
  create_time        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (report_id),
  KEY idx_paipan (paipan_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='深度报告';

CREATE TABLE IF NOT EXISTS t_order (
  order_id         BIGINT        NOT NULL AUTO_INCREMENT,
  order_no         VARCHAR(32)   NOT NULL,
  user_id          BIGINT        NOT NULL,
  total_amount     DECIMAL(10,2) NOT NULL,
  pay_amount       DECIMAL(10,2) DEFAULT NULL,
  discount_amount  DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  pay_type         TINYINT       DEFAULT NULL COMMENT '1微信 2支付宝 3Mock',
  pay_time         DATETIME      DEFAULT NULL,
  status           TINYINT       NOT NULL DEFAULT 0 COMMENT '0待支付 1已支付 2已取消 3已退款',
  coupon_id        BIGINT        DEFAULT NULL,
  create_time      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  update_time      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (order_id),
  UNIQUE KEY uk_order_no (order_no),
  KEY idx_order_user (user_id),
  KEY idx_order_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='订单';

CREATE TABLE IF NOT EXISTS t_order_item (
  item_id       BIGINT        NOT NULL AUTO_INCREMENT,
  order_id      BIGINT        NOT NULL,
  product_id    BIGINT        NOT NULL,
  product_name  VARCHAR(100)  NOT NULL,
  product_type  TINYINT       NOT NULL COMMENT '1排盘报告 2会员',
  ref_id        BIGINT        DEFAULT NULL COMMENT 'paipan_id when product_type=1',
  price         DECIMAL(10,2) NOT NULL,
  quantity      INT           NOT NULL DEFAULT 1,
  total_amount  DECIMAL(10,2) NOT NULL,
  PRIMARY KEY (item_id),
  KEY idx_order (order_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='订单明细';

CREATE TABLE IF NOT EXISTS t_membership (
  id             BIGINT        NOT NULL AUTO_INCREMENT,
  user_id        BIGINT        NOT NULL,
  level          TINYINT       NOT NULL DEFAULT 1,
  order_id       BIGINT        DEFAULT NULL,
  start_time     DATETIME      NOT NULL,
  end_time       DATETIME      NOT NULL,
  payment_amount DECIMAL(10,2) DEFAULT NULL,
  status         TINYINT       NOT NULL DEFAULT 1 COMMENT '0失效 1有效',
  create_time    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_membership_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='会员记录';

CREATE TABLE IF NOT EXISTS t_coupon (
  coupon_id    BIGINT        NOT NULL AUTO_INCREMENT,
  user_id      BIGINT        NOT NULL,
  coupon_code  VARCHAR(32)   NOT NULL,
  amount       DECIMAL(10,2) NOT NULL,
  min_amount   DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  expire_time  DATETIME      NOT NULL,
  use_time     DATETIME      DEFAULT NULL,
  order_id     BIGINT        DEFAULT NULL,
  type         TINYINT       NOT NULL DEFAULT 1,
  status       TINYINT       NOT NULL DEFAULT 0 COMMENT '0未使用 1已使用 2已过期',
  source       TINYINT       NOT NULL DEFAULT 1,
  create_time  DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (coupon_id),
  UNIQUE KEY uk_code (coupon_code),
  KEY idx_coupon_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='优惠券(预留)';
