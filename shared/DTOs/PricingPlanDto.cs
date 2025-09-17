using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs
{
    public class PricingPlanDto
    {
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Required]
        public decimal Price { get; set; }
        [Required]
        public string Currency { get; set; } = string.Empty;
        [Required]
        public string Interval { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new List<string>();
        public int MaxUsers { get; set; }
        public int MaxAppointments { get; set; }
        public bool IsActive { get; set; }
    }
}