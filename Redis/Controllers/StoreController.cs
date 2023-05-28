using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Redis.Data;
using Redis.Models;
using Redis.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Redis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoreController : ControllerBase
    {
        private readonly ICacheService _cacheService;
        private readonly AppDbContext _context;

        public StoreController(ICacheService cacheService, AppDbContext context)
        {
            _cacheService = cacheService;
            _context = context;
        }

        [HttpGet("Stores")]
        public async Task<IActionResult> Get()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Time time = new Time();
            var cacheDaTA = _cacheService.GetData<IEnumerable<Store>>("stores");
            if (cacheDaTA != null && cacheDaTA.Count() > 0)
            {
                stopwatch.Stop();
                time.Timelapsed = stopwatch.Elapsed.TotalMilliseconds.ToString();
                await _context.Times.AddAsync(time);
                await _context.SaveChangesAsync();
                return Ok(cacheDaTA);
            }
            cacheDaTA = await _context.Stores.ToListAsync();
            stopwatch.Stop();
            time.Timelapsed = stopwatch.Elapsed.TotalMilliseconds.ToString();
            var expiryTime = DateTimeOffset.Now.AddMinutes(10);
             _cacheService.SetData<IEnumerable<Store>>("stores", cacheDaTA, expiryTime);
            await _context.Times.AddAsync(time);
            await _context.SaveChangesAsync();
            return Ok(cacheDaTA);  
        }

        [HttpPost("AddStore")]
        public async Task<IActionResult> Post(string storeName)
        {
            Store store = new Store();
            store.Name = storeName;
            await _context.Stores.AddAsync(store);
            var expiryTime = DateTimeOffset.Now.AddMinutes(10);
            await _context.SaveChangesAsync();
            var cacheDaTA = await _context.Stores.ToListAsync();
            var isSuccess = _cacheService.SetData<IEnumerable<Store>>("stores", cacheDaTA, expiryTime);
            if(isSuccess)
                return Ok("Eklendi");
            return Ok(null);
        }

        [HttpDelete("DeleteStore")]
        public async Task<IActionResult> Delete(string name)
        {
            var deleteItem = await _context.Stores.FirstOrDefaultAsync(s => s.Name == name);
            if (deleteItem != null)
            {
                _context.Remove(deleteItem);
                _cacheService.RemoveData($"stores");
                await _context.SaveChangesAsync();
                return Ok();
            }
            return NotFound();
        }
    }
}
