using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Parqueo.Model.Entidades;

namespace Parqueo.Model.Contexto;

public partial class ParqueoContext : DbContext
{
    public ParqueoContext(DbContextOptions<ParqueoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<PrqAutomovil> PrqAutomovils { get; set; }

    public virtual DbSet<PrqIngresoAutomovil> PrqIngresoAutomovils { get; set; }

    public virtual DbSet<PrqParqueo> PrqParqueos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrqAutomovil>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_prq_automovil");

            entity.ToTable("prq_automovil", tb => tb.HasComment("Automóviles que utilizan los parqueos."));

            entity.Property(e => e.Id)
                .HasComment("Llave primaria del automóvil.")
                .HasColumnName("id");
            entity.Property(e => e.Anio)
                .HasComment("Año (modelo) del automóvil.")
                .HasColumnName("anio");
            entity.Property(e => e.Color)
                .HasMaxLength(30)
                .HasComment("Color del automóvil.")
                .HasColumnName("color");
            entity.Property(e => e.Fabricante)
                .HasMaxLength(50)
                .HasComment("Fabricante o marca del automóvil.")
                .HasColumnName("fabricante");
            entity.Property(e => e.Tipo)
                .HasMaxLength(10)
                .HasComment("Tipo de automóvil: sedán, 4x4 o moto.")
                .HasColumnName("tipo");
        });

        modelBuilder.Entity<PrqIngresoAutomovil>(entity =>
        {
            entity.HasKey(e => e.Consecutivo).HasName("pk_prq_ingreso_automovil");

            entity.ToTable("prq_ingreso_automovil", tb => tb.HasComment("Registro de ingresos y salidas de automóviles en los parqueos."));

            entity.HasIndex(e => e.FechaHoraEntrada, "ix_prq_ingreso_automovil_entrada");

            entity.HasIndex(e => e.IdAutomovil, "ix_prq_ingreso_automovil_id_automovil");

            entity.HasIndex(e => e.IdParqueo, "ix_prq_ingreso_automovil_id_parqueo");

            entity.Property(e => e.Consecutivo)
                .HasComment("Llave primaria consecutiva del ingreso.")
                .HasColumnName("consecutivo");
            entity.Property(e => e.FechaHoraEntrada)
                .HasComment("Fecha y hora de entrada.")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_hora_entrada");
            entity.Property(e => e.FechaHoraSalida)
                .HasComment("Fecha y hora de salida; NULL si el automóvil no ha salido.")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_hora_salida");
            entity.Property(e => e.IdAutomovil)
                .HasComment("Automóvil que ingresó.")
                .HasColumnName("id_automovil");
            entity.Property(e => e.IdParqueo)
                .HasComment("Parqueo al que ingresó el automóvil.")
                .HasColumnName("id_parqueo");

            entity.HasOne(d => d.IdAutomovilNavigation).WithMany(p => p.PrqIngresoAutomovils)
                .HasForeignKey(d => d.IdAutomovil)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_prq_ingreso_automovil_automovil");

            entity.HasOne(d => d.IdParqueoNavigation).WithMany(p => p.PrqIngresoAutomovils)
                .HasForeignKey(d => d.IdParqueo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_prq_ingreso_automovil_parqueo");
        });

        modelBuilder.Entity<PrqParqueo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_prq_parqueo");

            entity.ToTable("prq_parqueo", tb => tb.HasComment("Parqueos (estacionamientos) disponibles."));

            entity.Property(e => e.Id)
                .HasComment("Llave primaria del parqueo.")
                .HasColumnName("id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasComment("Nombre del parqueo.")
                .HasColumnName("nombre");
            entity.Property(e => e.NombreProvincia)
                .HasMaxLength(20)
                .HasComment("Provincia de Costa Rica donde se ubica el parqueo.")
                .HasColumnName("nombre_provincia");
            entity.Property(e => e.PrecioPorHora)
                .HasPrecision(10, 2)
                .HasComment("Tarifa por hora en colones.")
                .HasColumnName("precio_por_hora");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
