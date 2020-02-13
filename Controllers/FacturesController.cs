using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GrandHotel.Models;

namespace GrandHotel.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacturesController : ControllerBase
    {
        private readonly GrandHotelContext _context;

        public FacturesController(GrandHotelContext context)
        {
            _context = context;
        }
        
        // GET: api/Factures
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Facture>>> GetFacture()
        {
            return await _context.Facture.ToListAsync();
        }

        // GET: api/Factures/Client/5
        [HttpGet("client/{id}")]
        public async Task<ActionResult<List<Facture>>> GetFacturesClient(int id, [FromQuery]DateTime dateMin, [FromQuery]DateTime dateMax)
        {
            List<Facture> facture;

            //Vérifie si les deux dates sont renseignées, et les utilise. Sinon, prendre les factures sur un an glissant
            if (dateMin != DateTime.MinValue && dateMax != DateTime.MinValue)
                facture = await _context.Facture.Where(f => f.IdClient == id && f.DateFacture > dateMin && f.DateFacture < dateMax).ToListAsync();
            else
                facture = await _context.Facture.Where(f => f.IdClient == id && f.DateFacture > DateTime.Now.AddYears(-1)).ToListAsync();

            if (facture == null)
            {
                return NotFound();
            }

            return facture;
        }

        // GET: api/Factures/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Facture>> GetFacture(int id)
        {
            var facture = await _context.Facture.Include(f => f.LigneFacture).FirstOrDefaultAsync(f => f.Id == id);

            if (facture == null)
            {
                return NotFound();
            }

            return facture;
        }

        // PUT: api/Factures/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFacture(int id, Facture facture)
        {
            if (id != facture.Id)
            {
                return BadRequest();
            }

            _context.Entry(facture).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FactureExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Factures
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Facture>> PostFacture(Facture facture)
        {
            _context.Facture.Add(facture);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetFacture", new { id = facture.Id }, facture);
        }

        // POST: api/Factures
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost("details/{id}")]
        public async Task<ActionResult<LigneFacture>> PostDetail(int id, LigneFacture detail)
        {
            try
            {
                detail.IdFacture = id;

                _context.LigneFacture.Add(detail);
                await _context.SaveChangesAsync();

            } catch (Exception)
            {
                return BadRequest();
            }

            return CreatedAtAction("PostDetail", detail);
        }

        // DELETE: api/Factures/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Facture>> DeleteFacture(int id)
        {
            var facture = await _context.Facture.FindAsync(id);
            if (facture == null)
            {
                return NotFound();
            }

            _context.Facture.Remove(facture);
            await _context.SaveChangesAsync();

            return facture;
        }

        private bool FactureExists(int id)
        {
            return _context.Facture.Any(e => e.Id == id);
        }
    }
}
