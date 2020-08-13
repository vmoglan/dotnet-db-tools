namespace DbTools.Sqlite.Core.Encryption

open Microsoft.Data.Sqlite
open KeyOperations

module EncryptionUtils =
    [<Literal>]
    let DbOpenFailureErrorCode = 26

    let private tryPerformDbRead connectionString =
        try
            use connection = new SqliteConnection(connectionString)
            connection.Open()

            use command = connection.CreateCommand()
            command.CommandText <- "SELECT count(*) FROM sqlite_master"
            command.ExecuteScalar() |> ignore

            true
        with
        | :? SqliteException as sqliteException -> match sqliteException.SqliteErrorCode with
                                                   | DbOpenFailureErrorCode -> false
                                                   | _ -> raise sqliteException

    let testEncryption (dataSource, key) =
        let connectionString 
            = SqliteConnectionStringBuilder(DataSource = dataSource,
                                            Password = key,
                                            Mode = SqliteOpenMode.ReadOnly).ConnectionString
        
        connectionString |> tryPerformDbRead

    let isEncrypted dataSource = not ((dataSource, "") |> testEncryption)

    let encrypt (dataSource, key) =
        let connectionStringBuilder 
            = new SqliteConnectionStringBuilder(DataSource = dataSource, 
                                                Mode = SqliteOpenMode.ReadWriteCreate)
        let connectionString = connectionStringBuilder.ConnectionString

        use connection = new SqliteConnection(connectionString)
        connection.Open()

        connection |> executeKeyOperation KeyMethod.Key key
        connection |> executeKeyOperation KeyMethod.Rekey key

    let changeEncryptionKey (dataSource, currentKey, newKey) =
        let connectionStringBuilder 
            = new SqliteConnectionStringBuilder(DataSource = dataSource,
                                                Password = currentKey,
                                                Mode = SqliteOpenMode.ReadWrite)
        let connectionString = connectionStringBuilder.ConnectionString

        use connection = new SqliteConnection(connectionString)
        connection.Open()
        
        connection |> executeKeyOperation KeyMethod.Rekey newKey
        