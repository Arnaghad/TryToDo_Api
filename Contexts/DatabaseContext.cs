﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TryToDo_Api.Classes;

namespace TryToDo_Api.Contexts;

public class DatabaseContext : IdentityDbContext<AuthUser>
{
    public DbSet<Item> Items { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DatabaseContext()
    {
        Database.EnsureCreated();
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string connectionString = "server=localhost;user=root;password=953292529;database=todo;";
        optionsBuilder.UseMySql(
            connectionString, 
            ServerVersion.AutoDetect(connectionString)
        );
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // This line is crucial!

        modelBuilder.Entity<Item>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(i => i.CategoryId);


    }
}





