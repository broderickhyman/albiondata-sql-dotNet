-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Server version:               10.1.44-MariaDB-0ubuntu0.18.04.1 - Ubuntu 18.04
-- Server OS:                    debian-linux-gnu
-- HeidiSQL Version:             10.3.0.5771
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;


-- Dumping database structure for albion
CREATE DATABASE IF NOT EXISTS `albion` /*!40100 DEFAULT CHARACTER SET utf8mb4 */;
USE `albion`;

-- Dumping structure for table albion.gold_prices
CREATE TABLE IF NOT EXISTS `gold_prices` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `created_at` datetime(6) NOT NULL,
  `deleted_at` datetime(6) DEFAULT NULL,
  `price` int(10) unsigned NOT NULL,
  `timestamp` datetime(6) NOT NULL,
  `updated_at` datetime(6) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `IX_gold_prices_timestamp_deleted_at` (`timestamp`,`deleted_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Data exporting was unselected.

-- Dumping structure for table albion.market_history
CREATE TABLE IF NOT EXISTS `market_history` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `item_amount` bigint(20) unsigned NOT NULL,
  `silver_amount` bigint(20) unsigned NOT NULL,
  `timestamp` bigint(20) unsigned NOT NULL,
  `item_id` varchar(128) NOT NULL,
  `location` smallint(5) unsigned NOT NULL,
  `quality` tinyint(3) unsigned NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `Main` (`item_id`,`quality`,`location`,`timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Data exporting was unselected.

-- Dumping structure for table albion.market_orders
CREATE TABLE IF NOT EXISTS `market_orders` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `item_id` varchar(128) DEFAULT NULL,
  `location` smallint(5) unsigned NOT NULL,
  `quality_level` tinyint(3) unsigned NOT NULL,
  `enchantment_level` tinyint(3) unsigned NOT NULL,
  `price` bigint(20) unsigned NOT NULL,
  `amount` int(10) unsigned NOT NULL,
  `auction_type` varchar(32) DEFAULT NULL,
  `expires` datetime(6) NOT NULL,
  `albion_id` bigint(20) unsigned NOT NULL,
  `initial_amount` int(10) unsigned NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `updated_at` datetime(6) NOT NULL,
  `deleted_at` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `AlbionId` (`albion_id`),
  KEY `Deleted` (`deleted_at`),
  KEY `Expired` (`deleted_at`,`expires`,`updated_at`),
  KEY `TypeId` (`item_id`,`updated_at`,`deleted_at`),
  KEY `Main` (`item_id`,`auction_type`,`location`,`updated_at`,`deleted_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Data exporting was unselected.

-- Dumping structure for table albion.market_orders_expired
CREATE TABLE IF NOT EXISTS `market_orders_expired` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `item_id` varchar(128) DEFAULT NULL,
  `location` smallint(5) unsigned NOT NULL,
  `quality_level` tinyint(3) unsigned NOT NULL,
  `enchantment_level` tinyint(3) unsigned NOT NULL,
  `price` bigint(20) unsigned NOT NULL,
  `amount` int(10) unsigned NOT NULL,
  `auction_type` varchar(32) DEFAULT NULL,
  `expires` datetime(6) NOT NULL,
  `albion_id` bigint(20) unsigned NOT NULL,
  `initial_amount` int(10) unsigned NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `updated_at` datetime(6) NOT NULL,
  `deleted_at` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `AlbionId` (`albion_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Data exporting was unselected.

-- Dumping structure for table albion.market_stats
CREATE TABLE IF NOT EXISTS `market_stats` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `item_id` varchar(128) NOT NULL,
  `location` smallint(5) unsigned NOT NULL,
  `price_avg` decimal(65,30) NOT NULL,
  `price_max` bigint(20) unsigned NOT NULL,
  `price_min` bigint(20) unsigned NOT NULL,
  `timestamp` datetime(6) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `AK_market_stats_item_id_location_timestamp` (`item_id`,`location`,`timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Data exporting was unselected.

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
