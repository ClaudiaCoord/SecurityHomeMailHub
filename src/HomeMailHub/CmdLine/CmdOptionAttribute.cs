
using System;
using HomeMailHub.Properties;

namespace HomeMailHub.CmdLine
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.GenericParameter)]
    internal class CmdOptionAttribute : Attribute
    {
        private string StringFormat_ { get; set; } = default;
        private bool IsFileExists_ { get; set; } = false;

        public string Key { get; set; } = default;
        public string FileStringFormat { get => StringFormat_; set => StringFormat_ = value; }
        public string DirectoryStringFormat { get => StringFormat_; set => StringFormat_ = value; }

#       if CMDLINEARGS_USE_RESOURCE_DESCRIPTION
        public  string ResourceId { get; set; } = default;
        private string Desc_ = string.Empty;
        public  string Desc
        {
            get => string.IsNullOrWhiteSpace(ResourceId) ? Desc_ : Resources.ResourceManager.GetString(ResourceId);
            set => Desc_ = value;
        }
#       else
        public string ResourceId { get; set; } = default;
        public string Desc { get; set; } = default;
#       endif

        public bool IsFileExists { get => IsFileExists_; set => IsFileExists_ = value; }
        public bool IsDirectoryExists { get => IsFileExists_; set => IsFileExists_ = value; }

        public bool IsFile { get; set; } = false;
        public bool IsDirectory { get; set; } = false;
        public bool IsSwitch { get; set; } = false;
        public bool IsEnum { get; set; } = false;
    }
}
