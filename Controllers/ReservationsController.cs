using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GrandHotel.Models;
using Microsoft.Data.SqlClient;

namespace GrandHotel.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly GrandHotelContext _context;

        public ReservationsController(GrandHotelContext context)
        {
            _context = context;
        }

        // GET: api/Reservations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Reservation>>> GetReservation([FromQuery]DateTime date)
        {
            //Vérifie que la date a bien été passée en paramètre
            if (date == DateTime.MinValue)
                return BadRequest("Aucune date n'a été passée en paramètre");

            try
            {
                var reservations = await _context.Reservation.Where(r => r.Jour.Date == date.Date).ToListAsync();

                if (reservations.Count == 0)
                    return NotFound("Aucune réservations n'ont été trouvées pour cette date.");

                return reservations;
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        // GET: api/Reservations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<List<Reservation>>> GetReservationsClient(int id)
        {
            try
            {
                var reservation = await _context.Reservation.Where(r => r.IdClient == id).ToListAsync();

                if (reservation.Count() == 0)
                    return NotFound("Aucune réservations n'ont été trouvées pour ce client.");

                return reservation;
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        // PUT: api/Reservations/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut]
        public async Task<IActionResult> PutReservation(Reservation reservation)
        {
            try
            {
                if (_context.Reservation.Find(reservation.NumChambre, reservation.Jour).IdClient != reservation.IdClient)
                    return Conflict("Réservation déjà existante pour un client différent.");

                //On détache l'entité déjà sauvegardée en local, 
                var local = _context.Reservation.Local.FirstOrDefault(r => r.NumChambre == reservation.NumChambre & r.Jour == reservation.Jour);
                _context.Entry(local).State = EntityState.Detached;
                
                _context.Entry(reservation).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                await _context.Entry(reservation).ReloadAsync();
            }
            catch (Exception)
            {
                if (!ReservationExists(reservation.NumChambre, reservation.Jour))
                    return NotFound("Aucune réservation de cette chambre pour ce jour la.");
                else
                    return BadRequest();
            }

            return Ok(reservation);
        }

        // POST: api/Reservations
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Reservation>> PostReservation(Reservation reservation)
        {
            try
            {
                //Si la clé numChambre + jour existe déjà, renvoie un conflit
                if (ReservationExists(reservation.NumChambre, reservation.Jour))
                    return Conflict("Réservation déjà existante ce jour pour cette chambre.");

                _context.Reservation.Add(reservation);

                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                //Si l'erreur SqlException reçue a pour numéro 547, renvoie l'erreur associée
                SqlException sqle = (SqlException)e.InnerException;
                if (sqle.Number == 547)
                    return BadRequest("Cette date n'existe pas dans le calendrier");
                return BadRequest();
            }

            return CreatedAtAction("PostReservation", reservation);
        }

        // DELETE: api/Reservations/?num=1&date=01/01/2016
        [HttpDelete]
        public async Task<ActionResult<Reservation>> DeleteReservation([FromQuery]short num, [FromQuery]DateTime date)
        {
            //Vérifie la présence de la réservation en se basant sur ses clés primaires
            var reservation = await _context.Reservation.FindAsync(num, date);
            if (reservation == null)
            {
                return NotFound("Réservation inconnue ce jour pour cette chambre");
            }

            _context.Reservation.Remove(reservation);
            await _context.SaveChangesAsync();

            return reservation;
        }

        private bool ReservationExists(short id, DateTime jour)
        {
            return _context.Reservation.Any(e => e.NumChambre == id && e.Jour == jour);
        }
    }
}
