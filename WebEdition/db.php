<?php
/**
 * Datenbank Kapselung
 *
 * Datum: 03.12.2007
 * @author Jan Christoph Bernack
 *
 */

final class db {

	public static $queryCount = 0;
	public static $DEBUG_PRINT_STATEMENTS = false;
//	public static $DEBUG_PRINT_STATEMENTS = true;
	public static $connectionStatus = false;
	public static $connection;
    public static $DEBUG_LOG;

	/**
	 * Öffnet eine Verbindung zu einer MySQL-Datenbank und gibt bei Erfolg true zurück, ansonsten false
	 *
	 * @param string $url
	 * @param string $username
	 * @param string $password
	 * @param string $database
	 * @return bool
	 */
	public static function openConnection($url, $username, $password, $database) {
		self::$connection = mysql_connect($url, $username, $password);
		self::$connectionStatus = self::$connection == false ? false : true;
		if (!mysql_select_db($database, self::$connection)) {
			self::closeConnection();
        }
		return db::$connectionStatus;
	}

	/**
	 * Schließt die Verbindung zur MySQL-Datenbank und gibt false zurück
	 *
	 * @return bool
	 */
	public static function closeConnection() {
        if (self::$connectionStatus) {
            mysql_close(self::$connection);
        }
		self::$connectionStatus = false;
		return self::$connectionStatus;
	}

	/**
	 * Führt ein SQL-Query aus und gibt das Ergebnis zurück
	 * Weiterhin wird der Queryanzahl-Zähler erhöht und ggf. das gesamt Query ausgegeben
	 *
	 * @param string $statement
	 * @return resource
	 */
	public static function query($statement) {
		if (self::$DEBUG_PRINT_STATEMENTS) {
			self::$DEBUG_LOG .= $statement."<br>\n";
        }
		self::$queryCount++;
		return mysql_query($statement, self::$connection);
    }

    public static function getInsertId() {
        return mysql_insert_id(self::$connection);
    }

    public static function printDebugOutput() {
        if (self::$DEBUG_LOG != "") {
            print "<b>Database Log:</b><br>\n";
            print self::$DEBUG_LOG;
        }
    }

}

?>