﻿// <auto-generated />
using System;
using EmpyrionModWebHost.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EmpyrionModWebHost.Migrations.HistoryBook
{
    [DbContext(typeof(HistoryBookContext))]
    partial class HistoryBookContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024");

            modelBuilder.Entity("EmpyrionModWebHost.Models.HistoryBookOfPlayers", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Changed");

                    b.Property<string>("Name");

                    b.Property<bool>("Online");

                    b.Property<string>("Playfield");

                    b.Property<int>("PosX");

                    b.Property<int>("PosY");

                    b.Property<int>("PosZ");

                    b.Property<string>("SteamId");

                    b.Property<DateTime>("Timestamp");

                    b.HasKey("Id");

                    b.ToTable("Players");
                });

            modelBuilder.Entity("EmpyrionModWebHost.Models.HistoryBookOfStructures", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Changed");

                    b.Property<int>("EntityId");

                    b.Property<string>("Name");

                    b.Property<string>("Playfield");

                    b.Property<int>("PosX");

                    b.Property<int>("PosY");

                    b.Property<int>("PosZ");

                    b.Property<DateTime>("Timestamp");

                    b.HasKey("Id");

                    b.ToTable("Structures");
                });
#pragma warning restore 612, 618
        }
    }
}
