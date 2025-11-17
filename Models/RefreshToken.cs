using Microsoft.EntityFrameworkCore;

namespace ExaminationSystem.Models
{
    [Owned]
    public class RefreshToken
    {
        public string Token { get; set; }
        public DateTime ExpiredOn { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? RevokedOn { get; set; }
        
        public bool IsExpired => DateTime.UtcNow >= ExpiredOn;
        public bool IsActive => RevokedOn == null && !IsExpired;
    }
}
