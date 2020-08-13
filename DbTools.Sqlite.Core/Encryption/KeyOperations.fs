namespace DbTools.Sqlite.Core.Encryption

open Microsoft.Data.Sqlite

module internal KeyOperations =
    type KeyMethod = Key = 'K' | Rekey = 'R'

    let private getQuotedParameter parameter (connection : SqliteConnection) =
        let command = connection.CreateCommand()
        command.CommandText <- "SELECT quote(@parameter)"
        command.Parameters.AddWithValue("@parameter", parameter) |> ignore
        let quotedParameter = downcast command.ExecuteScalar() : string
        quotedParameter

    let executeKeyOperation keyOperation key connection =
        let keyOperationString = match keyOperation with
                                 | KeyMethod.Key -> "key"
                                 | KeyMethod.Rekey -> "rekey"
                                 | _ -> failwith "Invalid key operations."
        
        let quotedKey = connection |> getQuotedParameter key

        let command = connection.CreateCommand()
        command.CommandText <- "PRAGMA " + keyOperationString + " = " + quotedKey
        command.ExecuteNonQuery() |> ignore

