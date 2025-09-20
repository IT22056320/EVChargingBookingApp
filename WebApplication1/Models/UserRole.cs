namespace WebApplication1.Models
{
    /// <summary>
    /// Defines the available user roles in the EV Charging Station system
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Back-office users with system administration access
        /// </summary>
        Backoffice,
        
        /// <summary>
        /// Station operators who can access web and mobile applications
        /// </summary>
        StationOperator,
        
        /// <summary>
        /// EV owners who use the mobile application for bookings
        /// </summary>
        EVOwner
    }
}