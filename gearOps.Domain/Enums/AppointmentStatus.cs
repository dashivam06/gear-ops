namespace gearOps.Domain.Enums;
public enum AppointmentStatus 
{ 
    Pending,      // Customer booked, awaiting staff approval
    Confirmed,    // Staff approved the appointment
    InProgress,   // Vehicle is actively being serviced (optional transitional state)
    Completed,    // Service completed, appointment finished
    Cancelled,    // Appointment cancelled (by customer or staff rejection)
    NoShow        // Customer didn't show up for confirmed appointment
}
