using AICalendar.CORE.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AICalendar.DAL.Configurations;

public class UserSeedConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        var user1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        builder.HasData(
            new User
            {
                Id = user1Id,
                Email = "chidi.okonkwo@example.com",
                Username = "chidiokonkwo",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = null
            },
            new User
            {
                Id = user2Id,
                Email = "amina.bello@example.com",
                Username = "aminabello",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = null
            }
        );
    }
}
