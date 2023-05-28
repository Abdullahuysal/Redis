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
    public class ProductController : ControllerBase
    {

        private readonly ICacheService _cacheService;
        private readonly AppDbContext _context;
        public ProductController(ICacheService cacheService, AppDbContext context)
        {
            _cacheService = cacheService;
            _context = context;
        }

        [HttpGet("ProductsgetwithRedis")]
        public async Task<IActionResult> GetwithRedis()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Time time = new Time();
            var cacheDaTA = _cacheService.GetData<IEnumerable<Product>>("products");
            if (cacheDaTA != null && cacheDaTA.Count() > 0)
            {
                stopwatch.Stop();
                time.Timelapsed = stopwatch.Elapsed.TotalMilliseconds.ToString();
                await _context.Times.AddAsync(time);
                await _context.SaveChangesAsync();
                return Ok(cacheDaTA);
            }
            cacheDaTA = await _context.Products.ToListAsync();
            var expiryTime = DateTimeOffset.Now.AddMinutes(10);
            _cacheService.SetData<IEnumerable<Product>>("products", cacheDaTA, expiryTime);
            await _context.Times.AddAsync(time);
            await _context.SaveChangesAsync();
            return Ok(cacheDaTA);
        }


        [HttpGet("Get")]
        public async Task<IActionResult> Get()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Time time = new Time();
            List<Product> products = await _context.Products.ToListAsync();
            stopwatch.Stop();
            time.Timelapsed = stopwatch.Elapsed.TotalMilliseconds.ToString();
            await _context.Times.AddAsync(time);
            await _context.SaveChangesAsync();
            return Ok(products);
        }

    }
}
