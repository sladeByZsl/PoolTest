using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace AssetBundleMaster.Common
{
    public static class FileAccessWrappedFunction
    {
        /// <summary>
        /// Open / Create File wrapped func
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static FileStream OpenOrCreateFile(string filePath)
        {
            if(File.Exists(filePath))
            {
                return File.Open(filePath, FileMode.Open, FileAccess.ReadWrite);
            }
            else
            {
                string dirPath = Path.GetDirectoryName(filePath);       // check dir exists
                DirectoryInfo di = new DirectoryInfo(dirPath);
                if(di == null || !di.Exists)
                {
                    di = Directory.CreateDirectory(dirPath);
                }
                if(di != null && di.Exists)
                {
                    return File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
                }
            }
            return null;
        }
        /// <summary>
        /// How to write string to a file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="info"></param>
        /// <param name="append"></param>
        public static void WriteStringToFile(string filePath, string info, bool append = false, bool useDefaultEncoding = false)
        {
            using(var fs = OpenOrCreateFile(filePath))
            {
                if(fs != null)
                {
                    WriteStringToFile(fs, info, append, useDefaultEncoding);
                }
            }
        }
        /// <summary>
        /// How to write string to a file
        /// </summary>
        /// <param name="st"></param>
        /// <param name="info"></param>
        /// <param name="append"></param>
        public static void WriteStringToFile(Stream st, string info, bool append = true, bool useDefaultEncoding = false)
        {
            using(var writer = new StreamWriter(st, useDefaultEncoding ? System.Text.Encoding.Default : System.Text.Encoding.UTF8))
            {
                if(writer != null)
                {
                    if(append)
                    {
                        writer.BaseStream.Seek(writer.BaseStream.Length, SeekOrigin.Begin);
                    }
                    else
                    {
                        writer.BaseStream.Seek(0, SeekOrigin.Begin);
                        writer.BaseStream.SetLength(0);
                    }
                    writer.WriteLine(info);
                }
            }
        }
        /// <summary>
        /// Rename a file
        /// </summary>
        /// <param name="srcFile"></param>
        /// <param name="moveTo"></param>
        /// <param name="reName"></param>
        public static FileInfo ReNameFile(FileInfo srcFile, DirectoryInfo moveTo, string reName)
        {
            if(srcFile.Exists && moveTo.Exists)
            {
                string path = Path.Combine(moveTo.FullName, reName);
                if(File.Exists(path))
                {
                    File.Delete(path);
                }
                srcFile.MoveTo(path);
                return new FileInfo(path);
            }
            return srcFile;
        }
        /// <summary>
        /// Caculate file MD5
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string GetMD5(string filePath)
        {
            StringBuilder sb = new StringBuilder();
            using(System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
            {
                byte[] md5Bytes = md5.ComputeHash(System.IO.File.ReadAllBytes(filePath));
                for(int i = 0; i < md5Bytes.Length; i++)
                {
                    sb.Append(md5Bytes[i].ToString("x2"));
                }
            }
            return sb.ToString();
        }
    }
}

