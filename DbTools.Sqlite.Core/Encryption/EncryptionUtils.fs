namespace DbTools.Sqlite.Core.Encryption

open Microsoft.Data.Sqlite

module EncryptionUtils =
    [<Literal>]
    let DbOpenFailureErrorCode = 26

    type private KeyOperations = Key = 'K' | Rekey = 'R'

    let private getQuotedParameter parameter (connection : SqliteConnection) =
        let command = connection.CreateCommand()
        command.CommandText <- "SELECT quote(@parameter)"
        command.Parameters.AddWithValue("@parameter", parameter) |> ignore
        let quotedParameter = downcast command.ExecuteScalar() : string
        quotedParameter

    let private executeKeyOperation keyOperation key connection =
        let keyOperationString = match keyOperation with
                                 | KeyOperations.Key -> "key"
                                 | KeyOperations.Rekey -> "rekey"
                                 | _ -> failwith "Invalid key operations."
        
        let quotedKey = connection |> getQuotedParameter key

        let command = connection.CreateCommand()
        command.CommandText <- "PRAGMA " + keyOperationString + " = " + quotedKey
        command.ExecuteNonQuery() |> ignore

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

    let testEncryption dataSource key =
        let connectionString 
            = SqliteConnectionStringBuilder(DataSource = dataSource,
                                            Password = key,
                                            Mode = SqliteOpenMode.ReadOnly).ConnectionString
        
        connectionString |> tryPerformDbRead

    let isEncrypted dataSource = not (testEncryption dataSource "")

    let encrypt dataSource key =
        let connectionStringBuilder 
            = new SqliteConnectionStringBuilder(DataSource = dataSource, 
                                                Mode = SqliteOpenMode.ReadWriteCreate)
        let connectionString = connectionStringBuilder.ConnectionString

        use connection = new SqliteConnection(connectionString)
        connection.Open()

        connection |> executeKeyOperation KeyOperations.Key key
        connection |> executeKeyOperation KeyOperations.Rekey key

    let changeEncryptionKey dataSource currentKey newKey =
        let connectionStringBuilder 
            = new SqliteConnectionStringBuilder(DataSource = dataSource,
                                                Password = currentKey,
                                                Mode = SqliteOpenMode.ReadWrite)
        let connectionString = connectionStringBuilder.ConnectionString

        use connection = new SqliteConnection(connectionString)
        connection.Open()
        
        connection |> executeKeyOperation KeyOperations.Rekey newKey
        