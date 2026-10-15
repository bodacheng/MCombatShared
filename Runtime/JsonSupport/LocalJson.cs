using System.IO;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Json
{
    public static class LocalJson
    {
        public static void SaveToJsonFile_persistentDataPath(string subPath, string filename, string json)
        {
            //string wholePath = Path.Combine(Application.persistentDataPath, subPath);
            string wholePath;
            if (subPath != null)
            {
                if (!Directory.Exists(Application.persistentDataPath + "/" + subPath))
                {
                    //if it doesn't, create it
                    Directory.CreateDirectory(Application.persistentDataPath + "/" + subPath);
                }
                wholePath = Application.persistentDataPath + "/" + subPath + "/" + filename;
            }
            else
            {
                wholePath = Application.persistentDataPath + "/" + filename;
            }
            
            Debug.Log("尝试建立本地文件 : "+wholePath);
            
            string temporaryPath = wholePath + ".tmp";
            try
            {
                // Keep the previous complete file until the replacement has been written.
                File.WriteAllText(temporaryPath, json, System.Text.Encoding.UTF8);
                if (File.Exists(wholePath))
                {
                    try
                    {
                        File.Replace(temporaryPath, wholePath, wholePath + ".bak");
                    }
                    catch (NotSupportedException)
                    {
                        // Some Unity targets do not implement File.Replace. Keep a recoverable
                        // old file until the new one is in place on those targets.
                        var backupPath = wholePath + ".bak";
                        if (File.Exists(backupPath))
                        {
                            File.Delete(backupPath);
                        }
                        File.Move(wholePath, backupPath);
                        try
                        {
                            File.Move(temporaryPath, wholePath);
                        }
                        catch
                        {
                            File.Move(backupPath, wholePath);
                            throw;
                        }
                    }
                }
                else
                {
                    File.Move(temporaryPath, wholePath);
                }
                Debug.Log(wholePath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    try { File.Delete(temporaryPath); }
                    catch (Exception e) { Debug.LogWarning(e); }
                }
            }
        }
        
        public static void SaveInfoToJsonFile_dataPath(string subPath, string filename, string json)
        {
            //string wholePath = Path.Combine(Application.persistentDataPath, subPath);
            string wholePath;
            if (subPath != null)
            {
                if (!Directory.Exists(Application.dataPath + "/" + subPath))
                {
                    //if it doesn't, create it
                    Directory.CreateDirectory(Application.dataPath + "/" + subPath);
                }
                wholePath = Application.dataPath + "/" + subPath + "/" + filename;
            }
            else
            {
                wholePath = Application.dataPath + "/" + filename;
            }
            
            try
            {
                if (!File.Exists(wholePath))
                {
                    File.Create(wholePath).Close();
                }
                Debug.Log("文件生成"+ wholePath);
                File.WriteAllText(wholePath, json, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
        
        #if UNITY_EDITOR
        public static UnityEngine.Object LoadFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }
        
        public static void DeleteAllUnderFolder(string filePath)
        {
            try
            {
                if (Directory.Exists(filePath))
                {
                    foreach (string file in Directory.GetFiles(filePath))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
        #endif
    }
}
