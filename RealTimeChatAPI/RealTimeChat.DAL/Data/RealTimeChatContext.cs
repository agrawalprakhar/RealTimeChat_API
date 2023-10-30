using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using Microsoft.Identity.Client;
using RealTimeChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;


namespace RealTimeChat.DAL.Data
{
    public class RealTimeChatContext : IdentityDbContext<User>
    {
        public RealTimeChatContext(DbContextOptions<RealTimeChatContext> options) : base(options)
        {

           
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>().ToTable("User");

            builder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            builder.Entity<Message>().ToTable("Message");

            builder.Entity<Logs>().ToTable("Logs");

            builder.Entity<Message>()
             .HasOne(m => m.Receiver)
             .WithMany()
             .HasForeignKey(m => m.ReceiverId)
             .OnDelete(DeleteBehavior.NoAction);

            //configure sender
            builder.Entity<Message>()
              .HasOne(m => m.Sender)
              .WithMany()
              .HasForeignKey(m => m.SenderId)
              .OnDelete(DeleteBehavior.NoAction);

        }
        public DbSet<User> Users { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<Logs> Logs { get; set; }

        public DbSet<LastSeen> LastSeenRecords { get; set; }

    }
}
