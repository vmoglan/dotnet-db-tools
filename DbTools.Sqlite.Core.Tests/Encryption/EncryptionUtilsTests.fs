module DbTools.Sqlite.Core.Tests.Encryption.EncryptionUtilsTests

open NUnit.Framework
open System.IO
open System.Reflection
open System
open DbTools.Sqlite.Core.Encryption

let executionDirPath = Assembly.GetExecutingAssembly().Location
                       |> Path.GetDirectoryName

let appDataPath = Path.Combine(executionDirPath, "appdata")

[<SetUp>]
let setup () =
    match appDataPath |> Directory.Exists with
    | false -> appDataPath |> Directory.CreateDirectory |> ignore
    | true -> ()

[<
    Test; 
    Category("Local")
>]
let willCreateEncryptAndReEncryptDatabase () =
    let dbFileName = Path.ChangeExtension(Guid.NewGuid().ToString(), ".sqlite3")
    let dataSource = Path.Combine (appDataPath, dbFileName)
    let password = Guid.NewGuid().ToString()

    EncryptionUtils.encrypt dataSource password
    Assert.IsTrue(EncryptionUtils.isEncrypted dataSource)
    Assert.IsTrue(EncryptionUtils.testEncryption dataSource password)
