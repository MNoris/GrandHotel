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
    public class ClientsController : ControllerBase
    {
        private readonly GrandHotelContext _context;

        public ClientsController(GrandHotelContext context)
        {
            _context = context;
        }

        // GET: api/Clients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Client>>> GetClient()
        {
            return await _context.Client.ToListAsync();
        }

        // GET: api/Clients/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Client>> GetClient(int id)
        {
            // Récupère le client correspondant à l'ID passé en paramètre en incluant son adresse 
            var client = await _context.Client.Include(c => c.Adresse).Include(c => c.Telephone).FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            return client;
        }

        // PUT: api/Clients/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutClient(int id, Client client)
        {
            if (id != client.Id)
            {
                return BadRequest();
            }

            _context.Entry(client).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _context.Entry(client).Reload();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClientExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(client);
        }

        // POST: api/Clients
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Client>> PostClient(Client client)
        {
            try
            {
                //Ajoute le client dans un premier temps, puis son adresse
                _context.Client.Add(client);
                _context.Adresse.Add(client.Adresse);

                //Lors de la sauvegarde, l'id du client sera automatiquement assigné au nouveau client, ainsi qu'à son adresse
                await _context.SaveChangesAsync();

                return CreatedAtAction("PostClient", new { id = client.Id }, client);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        // POST: api/Clients/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost("{id}")]
        public async Task<ActionResult<Client>> PostTelephoneClient(int id, Telephone telephone)
        {
            try
            {
                //Vérifie que l'entité Telephone envoyée possède bien l'identifiant de client demandé associé.
                //Sinon, attribue celui passé en paramètre
                if (telephone.IdClient != id)
                    telephone.IdClient = id;

                _context.Telephone.Add(telephone);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbuE)
            {
                var sqlE = (SqlException)dbuE.InnerException;

                //Si le numéro d'exception correspond à 2627(Violation de contrainte de clé primaire), renvoie un message explicite
                if (sqlE.Number == 2627)
                {
                    return BadRequest("Le numéro de téléphone " + telephone.Numero + " est déjà utilisé, et ne peut être duppliqué.");
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("PostTelephoneClient", telephone);
        }

        // DELETE: api/Clients/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Client>> DeleteClient(int id)
        {
            //Récupère le client, son adresse et son / ses numéros de téléphone. Si null, retourne not found.
            var client = await _context.Client.Include(c => c.Adresse).Include(c => c.Telephone).Where(c => c.Id == id).FirstOrDefaultAsync();
            if (client == null)
            {
                return NotFound();
            }

            try
            {
                //Vérifie la présence de l'adresse et numéro(s), et si présents, les supprimer d'abord
                if (client.Adresse != null)
                {
                    _context.Adresse.Remove(client.Adresse);
                }

                if (client.Telephone != null)
                {
                    _context.Telephone.RemoveRange(client.Telephone);
                }

                _context.Client.Remove(client);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbue)
            {
                var sqlEx = (SqlException)dbue.InnerException;

                //Si le numéro d'exception correspond à 547(Violation de contrainte de clé étrangère), renvoie un message explicite
                if (sqlEx.Number == 547)
                    return BadRequest("Le client id " + id + " ne peut pas être supprimé, car il est utilisé ");
                
                throw;
            }

            return NoContent();
        }

        private bool ClientExists(int id)
        {
            return _context.Client.Any(e => e.Id == id);
        }
    }
}
