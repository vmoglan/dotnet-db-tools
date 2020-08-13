namespace DbTools.Sqlite.Core.Tests.Encryption

open NUnit.Framework
open System.IO
open System.Reflection
open System
open DbTools.Sqlite.Core.Encryption

module EncryptionUtilsTests =
    let executionDirPath = Assembly.GetExecutingAssembly().Location
                           |> Path.GetDirectoryName

    let appDataPath = Path.Combine(executionDirPath, "appdata")

    [<SetUp>]
    let Setup () =
        match appDataPath |> Directory.Exists with
        | false -> appDataPath |> Directory.CreateDirectory |> ignore
        | true -> ()

    [<
        Test; 
        Category("Local")
    >]
    let ShouldCreateEncryptAndReEncryptDatabase () =
        let dbFileName = Path.ChangeExtension(Guid.NewGuid().ToString(), ".sqlite3")
        let dataSource = Path.Combine (appDataPath, dbFileName)
        let initialKey = Guid.NewGuid().ToString()

        (dataSource, initialKey) |> EncryptionUtils.encrypt

        dataSource |> EncryptionUtils.isEncrypted |> Assert.IsTrue
        (dataSource, initialKey) |> EncryptionUtils.testEncryption |> Assert.IsTrue

        let newKey = Guid.NewGuid().ToString()
    
        (dataSource, initialKey, newKey) |> EncryptionUtils.changeEncryptionKey 

        dataSource |> EncryptionUtils.isEncrypted |> Assert.IsTrue
        (dataSource, initialKey) |> EncryptionUtils.testEncryption |> Assert.IsFalse
        (dataSource, newKey) |> EncryptionUtils.testEncryption |> Assert.IsTrue
