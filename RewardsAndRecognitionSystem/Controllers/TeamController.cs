﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.Rendering;

using Microsoft.EntityFrameworkCore;

using RewardsAndRecognitionRepository.Interfaces;

using RewardsAndRecognitionRepository.Models;
using RewardsAndRecognitionSystem.ViewModels;


namespace RewardsAndRecognitionSystem.Controllers

{

    //[Authorize(Roles = "Admin")]

    public class TeamController : Controller

    {
        private readonly IMapper _mapper;

        private readonly ITeamRepo _teamRepo;

        private readonly ApplicationDbContext _context;

        private readonly IUserRepo _userRepo;

        private readonly UserManager<User> _userManager;

        public TeamController(IMapper mapper,ITeamRepo teamRepo, IUserRepo userRepo, UserManager<User> userManager, ApplicationDbContext context)

        {
            _mapper = mapper;
            _teamRepo = teamRepo;

            _userRepo = userRepo;

            _context = context;

            _userManager = userManager;

        }

        // GET: TeamController

        public async Task<IActionResult> Index(int page=1)

        {

            int pageSize = 2;
            var teams = await _context.Teams
                .Include(t => t.TeamLead)
                .Include(t => t.Manager)
                .Include(t => t.Director)
                .Include(t => t.Users)
                .OrderBy(t => t.Name)
                .ToListAsync();

            var userRoles = new Dictionary<string, string>();
            foreach (var team in teams)
            {
                foreach (var user in team.Users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userRoles[user.Id] = roles.FirstOrDefault() ?? "N/A";
                }
            }

            var paginatedTeams = teams
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var grouped = paginatedTeams.Select(team => new GroupedTeam
            {
                Team = team,
                Users = team.Users?.ToList() ?? new List<User>()
            }).ToList();

            ViewBag.UserRoles = userRoles;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)teams.Count / pageSize);

            // If AJAX, return partial view
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_TeamListPartial", grouped);
            }

            return View(grouped);

        }





        // GET: TeamController/Create

        public async Task<IActionResult> Create()

        {

            //var managers = await _userRepo.GetAllManagersAsync();

            //var leads = await _userRepo.GetLeadsAsync(); // ⬅️ Only unassigned team leads

            //ViewBag.Managers = new SelectList(managers, "Id", "Name");

            //ViewBag.TeamLeads = new SelectList(leads, "Id", "Name");

            await LoadDropdownsAsync();

            return View();

        }


        // POST: TeamController/Create

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(TeamViewModel viewModel)
        {
            if (ModelState.IsValid)

            {
                var team=_mapper.Map<Team>(viewModel);
                await _teamRepo.AddAsync(team);

                return RedirectToAction(nameof(Index));

            }
            //ModelState.Clear();
            await LoadDropdownsAsync();

            return View(viewModel);

        }

        // GET: TeamController/Edit/5

        public async Task<IActionResult> Edit(Guid id)

        {
          

           if(ModelState.IsValid)
            {
                var existingteam = await _teamRepo.GetByIdAsync(id);
                var team = _mapper.Map<TeamViewModel>(existingteam);

                if (team == null)

                {

                    return NotFound();

                }

                var managers = await _userRepo.GetAllManagersAsync(); // ✅ Required

                var leads = await _userRepo.GetLeadsAsync(team.TeamLeadId);

                var directors = await _userRepo.GetAllDirectorsAsync();// ✅ Required

                ViewBag.Managers = new SelectList(managers, "Id", "Name", team.ManagerId);

                ViewBag.TeamLeads = new SelectList(leads, "Id", "Name", team.TeamLeadId);

                ViewBag.Directors = new SelectList(directors, "Id", "Name", team.DirectorId);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")

                {

                    return PartialView("Edit", team); // Use a partial view here

                }

                return View(team);
            }
            else
            {  return View(); }
        }


        // POST: TeamController/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, TeamViewModel updatedTeam)
        {
            if (id != updatedTeam.Id)
                return BadRequest();

            // 🔒 Fetch existing team from DB
            var existingTeam = await _teamRepo.GetByIdAsync(id);
            if (existingTeam == null)
                return NotFound();

            // ✅ Custom validation (example)
            // You can add specific rules here if needed

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(); 
                //ModelState.Clear(); 
                var teamViewModel=_mapper.Map<TeamViewModel>(existingTeam);
                return View(teamViewModel); 
            }

            // Check for team lead change
            if (existingTeam.TeamLeadId != updatedTeam.TeamLeadId)
            {
                // Unassign old team lead
                if (!string.IsNullOrEmpty(existingTeam.TeamLeadId))
                {
                    var oldLead = await _userRepo.GetByIdAsync(existingTeam.TeamLeadId);
                    if (oldLead != null)
                    {
                        oldLead.TeamId = null;
                        await _userRepo.UpdateAsync(oldLead);
                    }
                }

                // Assign new team lead
                if (!string.IsNullOrEmpty(updatedTeam.TeamLeadId))
                {
                    var newLead = await _userRepo.GetByIdAsync(updatedTeam.TeamLeadId);
                    if (newLead != null)
                    {
                        newLead.TeamId = existingTeam.Id;
                        await _userRepo.UpdateAsync(newLead);
                    }
                }
            }

            // Only update the editable fields
            existingTeam.Name = updatedTeam.Name;
            existingTeam.TeamLeadId = updatedTeam.TeamLeadId;
            existingTeam.ManagerId = updatedTeam.ManagerId;
            existingTeam.DirectorId = updatedTeam.DirectorId;

            // ✅ Save changes
            await _teamRepo.UpdateAsync(existingTeam);
            return RedirectToAction(nameof(Index));
        }



        // GET: TeamController/Details/5

        public async Task<IActionResult> Details(Guid id)

        {

            var team = await _teamRepo.GetByIdAsync(id);

            if (team == null)

                return NotFound();

            return View(team);

        }

        // GET: TeamController/Delete/5

        public async Task<IActionResult> Delete(Guid id)

        {

            var team = await _teamRepo.GetByIdAsync(id);

            if (team == null)

                return NotFound();

            return View(team);

        }

        // POST: TeamController/Delete/5

        [HttpPost, ActionName("Delete")]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteConfirmed(Guid id)

        {

            var team = await _teamRepo.GetByIdAsync(id);

            if (team == null)

                return NotFound();

            await _teamRepo.DeleteAsync(team);

            return RedirectToAction(nameof(Index));

        }

        private async Task LoadDropdownsAsync()

        {

            var managers = await _userRepo.GetAllManagersAsync();

            var leads = await _userRepo.GetLeadsAsync();

            var directors = await _userRepo.GetAllDirectorsAsync();

            ViewBag.Managers = new SelectList(managers, "Id", "Name");

            ViewBag.TeamLeads = new SelectList(leads, "Id", "Name");

            ViewBag.Directors = new SelectList(directors, "Id", "Name");

        }

    }

}

