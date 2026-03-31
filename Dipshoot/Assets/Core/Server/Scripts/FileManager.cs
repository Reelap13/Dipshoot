using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace SavingSystem
{
    public static class FileManager
    {
        public static void SaveFile<T>(string folder_name, string file_name, T data)
        {
            string folder_path = Path.Combine(Application.persistentDataPath, folder_name);
            string file_path = Path.Combine(folder_path, file_name);

            if (!Directory.Exists(folder_path))
            {
                Directory.CreateDirectory(folder_path);
            }

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream file_stream = File.Create(file_path);
            formatter.Serialize(file_stream, data);
            file_stream.Close();
        }

        public static T LoadFile<T>(string folder_name, string file_name)
        {
            string folder_path = Path.Combine(Application.persistentDataPath, folder_name);
            string file_path = Path.Combine(folder_path, file_name);

            if (File.Exists(file_path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream file_stream = File.Open(file_path, FileMode.Open);
                T loaded_data = (T)formatter.Deserialize(file_stream);
                file_stream.Close();
                return loaded_data;
            }
            else
                return default(T);
        }

        public static bool IsFileExists(string folder_name, string file_name)
        {
            string folder_path = Path.Combine(Application.persistentDataPath, folder_name);
            string file_path = Path.Combine(folder_path, file_name);

            return File.Exists(file_path);
        }

        public static bool IsFolderExists(string folder_name)
        {
            string folder_path = Path.Combine(Application.persistentDataPath, folder_name);
            return Directory.Exists(folder_path);
        }

        public static void CreateFolder(string folder_name)
        {
            string folder_path = Path.Combine(Application.persistentDataPath, folder_name);
            if (!Directory.Exists(folder_path))
                Directory.CreateDirectory(folder_path);
        }

        public static void DeleteFolder(string folder_name)
        {
            string folder_path = Path.Combine(Application.persistentDataPath, folder_name);
            if (Directory.Exists(folder_path))
                Directory.Delete(folder_path, true);
        }
    }
}