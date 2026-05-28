using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{

    public static class LogLevel
    {
        public const int Error = 0;
        public const int Warning = 50;
        public const int Summary = 100;
        public const int Audit = 150;
    }
}