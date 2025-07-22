﻿using RewardsAndRecognitionRepository.Models;

namespace RewardsAndRecognitionSystem.ViewModels
{
    public class CategoryViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; }
        public bool isActive { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Nomination>? Nominations { get; set; }

    }
}
