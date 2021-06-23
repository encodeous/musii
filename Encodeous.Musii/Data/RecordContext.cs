using System.Collections.Generic;
using DSharpPlus.Lavalink;
using Encodeous.Musii.Network;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Encodeous.Musii.Data
{
    public class RecordContext : DbContext
    {
        public DbSet<PlayerRecord> Records { get; set; }

        // The following configures EF to create a Sqlite database file as `C:\blogging.db`.
        // For Mac or Linux, change this to `/tmp/blogging.db` or any other absolute path.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite(@"Data Source=Records.db");
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };
            
            modelBuilder.Entity<PlayerRecord>()
                .Property(e => e.Tracks)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v, settings),
                    v => JsonConvert.DeserializeObject<IReadOnlyList<BaseMusicSource>>(v, settings));
            modelBuilder.Entity<PlayerRecord>()
                .Property(e => e.CurrentTrack)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v, settings),
                    v => JsonConvert.DeserializeObject<BaseMusicSource>(v, settings));
            modelBuilder.Entity<PlayerRecord>().HasKey(x => x.RecordId);
        }
    }
}