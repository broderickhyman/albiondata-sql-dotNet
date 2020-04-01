-- --------------------------------------------------------
-- Host:                         localhost
-- Server version:               10.3.22-MariaDB-1:10.3.22+maria~bionic - mariadb.org binary distribution
-- Server OS:                    debian-linux-gnu
-- HeidiSQL Version:             10.3.0.5771
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;


-- Dumping database structure for albion
CREATE DATABASE IF NOT EXISTS `albion` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `albion`;

-- Dumping structure for table albion.gold_prices
CREATE TABLE IF NOT EXISTS `gold_prices` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `timestamp` timestamp NULL DEFAULT NULL,
  `price` int(11) unsigned DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_gold_prices_timestamp` (`timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.

-- Dumping structure for table albion.market_history
CREATE TABLE IF NOT EXISTS `market_history` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `item_amount` bigint(20) unsigned NOT NULL,
  `silver_amount` bigint(20) unsigned NOT NULL,
  `item_id` varchar(128) NOT NULL,
  `location` smallint(5) unsigned NOT NULL,
  `quality` tinyint(3) unsigned NOT NULL,
  `timestamp` datetime(6) NOT NULL,
  `aggregation` tinyint(4) NOT NULL DEFAULT 6,
  PRIMARY KEY (`id`),
  UNIQUE KEY `Main` (`item_id`,`quality`,`location`,`timestamp`,`aggregation`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Data exporting was unselected.

-- Dumping structure for table albion.market_orders
CREATE TABLE IF NOT EXISTS `market_orders` (
  `albion_id` bigint(20) unsigned NOT NULL,
  `item_id` varchar(255) DEFAULT NULL,
  `quality_level` tinyint(3) unsigned DEFAULT NULL,
  `enchantment_level` tinyint(3) unsigned DEFAULT NULL,
  `price` bigint(20) unsigned DEFAULT NULL,
  `initial_amount` int(11) unsigned DEFAULT NULL,
  `amount` int(11) unsigned DEFAULT NULL,
  `auction_type` varchar(255) DEFAULT NULL,
  `expires` timestamp NULL DEFAULT NULL,
  `location` smallint(5) unsigned NOT NULL,
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uix_market_orders_albion_id` (`albion_id`),
  KEY `deleted` (`deleted_at`),
  KEY `expired` (`deleted_at`,`expires`,`updated_at`),
  KEY `main` (`item_id`,`location`,`updated_at`,`deleted_at`),
  KEY `expires` (`expires`),
  KEY `updated_at` (`updated_at`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.

-- Dumping structure for table albion.market_orders_expired
CREATE TABLE IF NOT EXISTS `market_orders_expired` (
  `albion_id` bigint(20) unsigned NOT NULL,
  `item_id` varchar(255) DEFAULT NULL,
  `quality_level` tinyint(3) unsigned DEFAULT NULL,
  `enchantment_level` tinyint(3) unsigned DEFAULT NULL,
  `price` bigint(20) unsigned DEFAULT NULL,
  `initial_amount` int(11) unsigned DEFAULT NULL,
  `amount` int(11) unsigned DEFAULT NULL,
  `auction_type` varchar(255) DEFAULT NULL,
  `expires` timestamp NULL DEFAULT NULL,
  `location` smallint(5) unsigned NOT NULL,
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uix_market_orders_expired_albion_id` (`albion_id`),
  KEY `updated_at_expired` (`updated_at`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 ROW_FORMAT=DYNAMIC;

-- Data exporting was unselected.

-- Dumping structure for table albion.market_stats
CREATE TABLE IF NOT EXISTS `market_stats` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `item_id` varchar(255) NOT NULL,
  `location` smallint(5) unsigned NOT NULL,
  `price_min` bigint(20) unsigned DEFAULT NULL,
  `price_max` bigint(20) unsigned DEFAULT NULL,
  `price_avg` decimal(10,0) DEFAULT NULL,
  `timestamp` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `item_id_location_timestamp_unique` (`item_id`,`location`,`timestamp`) USING BTREE,
  KEY `item_id` (`item_id`),
  KEY `location` (`location`),
  KEY `timestamp` (`timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
