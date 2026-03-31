using System;
using SavingSystem;
using UnityEngine;

namespace Server.ClientSide
{
    public static class ClientIdentity
    {
        private static string SAVE_FOLDER = "CLIENT_INFO";
        private static string SAVE_FILE = "CLIENT_ID";

        private static Guid _client_id;

        public static Guid ÑlientId
        {
            get
            {
                if (_client_id != Guid.Empty)
                    return _client_id;

                _client_id = LoadId();
                if (_client_id != Guid.Empty)
                    return _client_id;

                _client_id = GenerateId();
                SaveId(_client_id);
                return _client_id;
            }
        }

        private static bool IsHasSavedId()
        {
            return FileManager.IsFolderExists(SAVE_FOLDER) && 
                FileManager.IsFileExists(SAVE_FOLDER, SAVE_FILE);
        }

        private static Guid LoadId()
        {
            if (!IsHasSavedId())
                return Guid.Empty;
            return FileManager.LoadFile<Guid>(SAVE_FOLDER, SAVE_FILE);
        }

        private static void SaveId(Guid id)
        {
            if (!FileManager.IsFolderExists(SAVE_FOLDER))
                FileManager.CreateFolder(SAVE_FOLDER);

            FileManager.SaveFile(SAVE_FOLDER, SAVE_FILE, id);
        }

        public static Guid GenerateId()
        {
            return Guid.NewGuid();
        }
    }
}