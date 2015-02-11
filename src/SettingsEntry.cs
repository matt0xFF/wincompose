//
// WinCompose � a compose key for Windows
//
// Copyright: (c) 2013-2014 Sam Hocevar <sam@hocevar.net>
//                     2014 Benjamin Litzelmann
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What the Fuck You Want
//   to Public License, Version 2, as published by the WTFPL Task Force.
//   See http://www.wtfpl.net/ for more details.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace WinCompose
{
    /// <summary>
    /// The base class to represent an entry in the settings file. It handles thread-safe and process-safe saving and loading.
    /// </summary>
    public abstract class SettingsEntry
    {
        private const string MutexName = "wincompose-{1342C5FF-9483-45F3-BE0C-1C8D63CEA81C}";     
        private static readonly Mutex SettingsMutex = new Mutex(false, MutexName);

        /// <summary>
        /// Gets or sets the section of this settings entry.
        /// </summary>
        public string Section { get; set; }

        /// <summary>
        /// Gets or sets the key identifying this settings entry.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the value of this settings entry.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Saves this settings entry into the settings file. This operation is thread-safe and process-safe.
        /// </summary>
        /// <returns>A <see cref="bool"/> indicating whether the operation was successful.</returns>
        public bool Save()
        {
            if (SettingsMutex.WaitOne(2000))
            {
                try
                {
                    var stringVal = Serialize(Value);
                    var result = WritePrivateProfileString(Section, Key, stringVal, Settings.GetConfigFile());
                    return result == 0;
                }
                finally
                {
                    // Ensure the mutex is always released even if an exception is thrown
                    SettingsMutex.ReleaseMutex();
                }
            }
            return false;
        }

        /// <summary>
        /// Loads this settings entry from the settings file. This operation is thread-safe and process-safe.
        /// </summary>
        /// <returns>A <see cref="bool"/> indicating whether the operation was successful.</returns>
        public bool Load()
        {
            const int len = 255;

            if (SettingsMutex.WaitOne(2000))
            {
                try
                {
                    var stringBuilder = new StringBuilder(len);
                    var result = GetPrivateProfileString(Section, Key, "", stringBuilder, len, Settings.GetConfigFile());
                    if (result != 0)
                        return false;

                    var strVal = stringBuilder.ToString();
                    Value = Deserialize(strVal);
                    return true;
                }
                finally
                {
                    // Ensure the mutex is always released even if an exception is thrown
                    SettingsMutex.ReleaseMutex();
                }
            }
            return false;
        }

        /// <summary>
        /// Serializes the given value into a <see cref="string"/>. This method should not throw any unhandled exception.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>A string representing the given value.</returns>
        protected abstract string Serialize(object value);

        /// <summary>
        /// Deserializes the given string into an object of the type of this entry. This method should not throw any unhandled exception.
        /// </summary>
        /// <param name="str">The string to deserialize.</param>
        /// <returns>An instance of the type of this entry.</returns>
        protected abstract object Deserialize(string str);

        [DllImport("kernel32")]
        static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        [DllImport("kernel32")]
        static extern int GetPrivateProfileString(string section, string key, string @default, StringBuilder retVal, int size, string filePath);
    }

    /// <summary>
    /// A generic implementation of the <see cref="SettingsEntry"/> class. It handles serialization for most of the .NET built-in types.
    /// </summary>
    /// <typeparam name="T">The type of data this entry contains.</typeparam>
    public class SettingsEntry<T> : SettingsEntry
    {
        /// <summary>
        /// Gets or sets the value of this settings entry.
        /// </summary>
        public new T Value { get; set; }

        /// <inheritdoc/>
        protected override string Serialize(object value)
        {
            // The default implementation of Serialize just uses the ToString method
            return value == null ? string.Empty : value.ToString();
        }

        /// <inheritdoc/>
        protected override object Deserialize(string str)
        {
            try
            {
                // The default implementation of Deserialize uses the Convert class.
                return (T)Convert.ChangeType(str, typeof(T));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

}