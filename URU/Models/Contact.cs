using System;
using System.ComponentModel.DataAnnotations;

namespace URU.Models
{
    public class Contact
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime DateTime { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public string Name { get; set; }

        public string PhoneNumber { get; set; }

        [Required]
        public string Subject { get; set; }
    }
}