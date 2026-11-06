using Microsoft.EntityFrameworkCore;
using Npgsql;
using RxBox.Core.Entities;
using RxBox.Core.Enums;

namespace RxBox.Tests
{
    [Collection(ApiCollection.Nombre)]
    public class EsquemaTests(RxBoxApiFactory factory)
    {
        private const string ViolaFk = PostgresErrorCodes.ForeignKeyViolation;
        private const string ViolaUq = PostgresErrorCodes.UniqueViolation;
        private const string ViolaCheck = PostgresErrorCodes.CheckViolation;

        [Fact]
        public async Task Una_fk_compuesta_rechaza_referenciar_una_fila_de_otro_box()
        {
            var boxA = await factory.CrearBoxAsync();
            var personaDeB = await factory.CrearPersonaAsync(await factory.CrearBoxAsync());

            await AssertRechazaAsync(ViolaFk, new ContactoEmergencia
            {
                BoxId = boxA.Id,
                PersonaId = personaDeB.Id,
                Nombre = "Mamá",
                Telefono = "6620000000"
            });
        }

        [Fact]
        public async Task Una_fk_compuesta_opcional_tambien_rechaza_otro_box()
        {
            var boxA = await factory.CrearBoxAsync();
            var coachDeB = await factory.CrearPersonaAsync(await factory.CrearBoxAsync());

            await AssertRechazaAsync(ViolaFk, Programacion(boxA, coachId: coachDeB.Id));
        }

        [Fact]
        public async Task Una_fk_compuesta_opcional_acepta_null()
        {
            var box = await factory.CrearBoxAsync();

            await factory.EnDbAsync(db => db.ClaseProgramaciones.Add(Programacion(box)));
        }

        [Fact]
        public async Task Usuario_boxes_rechaza_una_persona_de_otro_box()
        {
            var boxA = await factory.CrearBoxAsync();
            var boxB = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(boxA);
            var personaDeA = await factory.CrearPersonaAsync(boxA);

            await AssertRechazaAsync(ViolaFk, new UsuarioBox
            {
                UsuarioId = usuario.Id,
                BoxId = boxB.Id,
                PersonaId = personaDeA.Id,
                Rol = RolUsuario.COACH
            });
        }

        [Fact]
        public async Task El_email_es_unico_sin_distinguir_mayusculas()
        {
            var usuario = await factory.CrearUsuarioAsync();

            await AssertRechazaAsync(ViolaUq, new Usuario { Email = usuario.Email.ToUpperInvariant(), PasswordHash = "x" });
        }

        [Fact]
        public async Task Tipo_membresia_exige_visitas_si_no_es_ilimitada()
        {
            var box = await factory.CrearBoxAsync();

            await AssertRechazaAsync(ViolaCheck, TipoMembresia(box, TipoCobertura.PAQUETE, visitas: null));
            await factory.EnDbAsync(db => db.TiposMembresia.Add(TipoMembresia(box, TipoCobertura.ILIMITADA, visitas: null)));
            await factory.EnDbAsync(db => db.TiposMembresia.Add(TipoMembresia(box, TipoCobertura.PAQUETE, visitas: 10)));
        }

        [Fact]
        public async Task Restriccion_es_programacion_u_horario_con_dias_entre_1_y_7()
        {
            var box = await factory.CrearBoxAsync();
            var tipo = TipoMembresia(box, TipoCobertura.ILIMITADA, visitas: null);
            var programacion = Programacion(box);
            await factory.EnDbAsync(db =>
            {
                db.TiposMembresia.Add(tipo);
                db.ClaseProgramaciones.Add(programacion);
            });

            TipoMembresiaRestriccion Restriccion(int? programacionId, TimeOnly? inicio, TimeOnly? fin, short[]? dias) => new()
            {
                BoxId = box.Id,
                TipoMembresiaId = tipo.Id,
                ProgramacionId = programacionId,
                HoraInicio = inicio,
                HoraFin = fin,
                DiasSemana = dias
            };
            var seis = new TimeOnly(6, 0);
            var ocho = new TimeOnly(8, 0);

            await AssertRechazaAsync(ViolaCheck, Restriccion(null, null, null, null));
            await AssertRechazaAsync(ViolaCheck, Restriccion(programacion.Id, seis, ocho, null));
            await AssertRechazaAsync(ViolaCheck, Restriccion(null, ocho, seis, null));
            await AssertRechazaAsync(ViolaCheck, Restriccion(null, seis, ocho, [1, 8]));
            await factory.EnDbAsync(db => db.TipoMembresiaRestricciones.Add(Restriccion(programacion.Id, null, null, null)));
            await factory.EnDbAsync(db => db.TipoMembresiaRestricciones.Add(Restriccion(null, seis, ocho, [1, 3, 5])));

            var dias = await LeerDiasAsync(box, tipo);
            Assert.Equal([1, 3, 5], dias);
        }

        [Fact]
        public async Task Una_inscripcion_cancelada_permite_volver_a_inscribirse()
        {
            var box = await factory.CrearBoxAsync();
            var persona = await factory.CrearPersonaAsync(box);
            var clase = new Clase
            {
                BoxId = box.Id,
                Fecha = new DateOnly(2026, 1, 5),
                Nombre = "WOD",
                HoraInicio = new TimeOnly(6, 0),
                HoraFin = new TimeOnly(7, 0),
                Cupo = 10
            };
            await factory.EnDbAsync(db => db.Clases.Add(clase));

            Inscripcion Inscripcion(EstadoInscripcion estado) => new()
            {
                BoxId = box.Id,
                ClaseId = clase.Id,
                PersonaId = persona.Id,
                Estado = estado,
                CanalReserva = CanalReserva.COACH,
                FechaReserva = DateTimeOffset.UtcNow
            };

            await factory.EnDbAsync(db => db.Inscripciones.Add(Inscripcion(EstadoInscripcion.CANCELADO)));
            await factory.EnDbAsync(db => db.Inscripciones.Add(Inscripcion(EstadoInscripcion.INSCRITO)));
            await AssertRechazaAsync(ViolaUq, Inscripcion(EstadoInscripcion.INSCRITO));
        }

        [Fact]
        public async Task Dos_clases_de_la_misma_programacion_no_comparten_fecha()
        {
            var box = await factory.CrearBoxAsync();
            var programacion = Programacion(box);
            await factory.EnDbAsync(db => db.ClaseProgramaciones.Add(programacion));

            Clase Clase(int? programacionId) => new()
            {
                BoxId = box.Id,
                ProgramacionId = programacionId,
                Fecha = new DateOnly(2026, 1, 5),
                Nombre = "WOD",
                HoraInicio = new TimeOnly(6, 0),
                HoraFin = new TimeOnly(7, 0),
                Cupo = 10
            };

            await factory.EnDbAsync(db => db.Clases.Add(Clase(programacion.Id)));
            await AssertRechazaAsync(ViolaUq, Clase(programacion.Id));
            await factory.EnDbAsync(db => db.Clases.AddRange(Clase(null), Clase(null)));
        }

        private async Task<short[]?> LeerDiasAsync(Box box, TipoMembresia tipo)
        {
            short[]? dias = null;
            await factory.EnDbAsync(db => dias = db.TipoMembresiaRestricciones.IgnoreQueryFilters()
                .Where(r => r.BoxId == box.Id && r.TipoMembresiaId == tipo.Id && r.DiasSemana != null)
                .Select(r => r.DiasSemana)
                .Single());
            return dias;
        }

        private async Task AssertRechazaAsync(string sqlState, object entidad)
        {
            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => factory.EnDbAsync(db => db.Add(entidad)));

            Assert.Equal(sqlState, Assert.IsType<PostgresException>(ex.InnerException).SqlState);
        }

        private static ClaseProgramacion Programacion(Box box, int? coachId = null) => new()
        {
            BoxId = box.Id,
            Nombre = "6 AM",
            HoraInicio = new TimeOnly(6, 0),
            HoraFin = new TimeOnly(7, 0),
            CoachId = coachId,
            Cupo = 10,
            VigenteDesde = new DateOnly(2026, 1, 1)
        };

        private static TipoMembresia TipoMembresia(Box box, TipoCobertura cobertura, int? visitas) => new()
        {
            BoxId = box.Id,
            Nombre = Guid.NewGuid().ToString("N"),
            TipoCobertura = cobertura,
            Monto = 800,
            DuracionDias = 30,
            VisitasIncluidas = visitas,
            ExpiraDesde = ExpiraDesde.COMPRA
        };
    }
}
