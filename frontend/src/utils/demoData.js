// Demo data for testing the application when backend is not available
export const demoGitHubData = {
  profile: {
    login: "octocat",
    name: "The Octocat",
    avatarUrl: "https://github.com/octocat.png",
    bio: "A passionate developer who loves open source",
    location: "San Francisco",
    blog: "https://github.com/blog",
    htmlUrl: "https://github.com/octocat",
    followers: 1000,
    following: 100,
    createdAt: "2021-01-01T00:00:00Z"
  },
  repositories: [
    {
      id: 1,
      name: "Hello-World",
      description: "My first repository on GitHub!",
      language: "JavaScript",
      stargazersCount: 150,
      forksCount: 25,
      size: 2048,
      updatedAt: "2024-11-01T00:00:00Z",
      htmlUrl: "https://github.com/octocat/Hello-World"
    },
    {
      id: 2,
      name: "React-Dashboard",
      description: "A beautiful dashboard built with React and TypeScript",
      language: "TypeScript",
      stargazersCount: 320,
      forksCount: 45,
      size: 15000,
      updatedAt: "2024-10-15T00:00:00Z",
      htmlUrl: "https://github.com/octocat/React-Dashboard"
    },
    {
      id: 3,
      name: "Python-ML-Project",
      description: "Machine learning project using Python and scikit-learn",
      language: "Python",
      stargazersCount: 89,
      forksCount: 12,
      size: 8500,
      updatedAt: "2024-09-20T00:00:00Z",
      htmlUrl: "https://github.com/octocat/Python-ML-Project"
    }
  ],
  stats: {
    totalRepos: 15,
    totalStars: 559,
    languageStats: {
      "JavaScript": 8,
      "TypeScript": 4,
      "Python": 2,
      "Java": 1
    },
    commitActivity: [
      { week: 1699200000, total: 12 },
      { week: 1699804800, total: 18 },
      { week: 1700409600, total: 22 },
      { week: 1701014400, total: 15 },
      { week: 1701619200, total: 9 },
      { week: 1702224000, total: 25 },
      { week: 1702828800, total: 31 }
    ]
  }
}

export const demoAiAnalysis = {
  summary: "This developer shows strong proficiency in modern web development with a focus on JavaScript ecosystem. They demonstrate consistent contribution patterns and work on diverse projects ranging from frontend applications to machine learning. Their code quality appears high based on repository structure and documentation.",
  experienceLevel: "Mid-Level",
  skills: ["JavaScript", "TypeScript", "React", "Python", "Node.js", "Git", "HTML/CSS"]
}

export const demoJobs = [
  {
    id: 1,
    title: "Frontend React Developer",
    company: "TechCorp Inc",
    location: "San Francisco, CA",
    type: "Full-time",
    experienceLevel: "Mid-Level",
    description: "We're looking for a talented React developer to join our frontend team. You'll work on building user interfaces for our web applications using modern technologies.",
    minSalary: 90000,
    maxSalary: 120000,
    requiredSkills: ["React", "JavaScript", "TypeScript", "HTML", "CSS", "Git"],
    postedDate: "2024-11-01T00:00:00Z"
  },
  {
    id: 2,
    title: "Full Stack JavaScript Developer",
    company: "StartupXYZ",
    location: "Remote",
    type: "Full-time",
    experienceLevel: "Senior",
    description: "Join our fast-growing startup as a full stack developer. Work with React, Node.js, and modern cloud technologies to build scalable applications.",
    minSalary: 110000,
    maxSalary: 140000,
    requiredSkills: ["React", "Node.js", "JavaScript", "TypeScript", "AWS", "MongoDB"],
    postedDate: "2024-10-28T00:00:00Z"
  },
  {
    id: 3,
    title: "Junior Python Developer",
    company: "DataScience Solutions",
    location: "New York, NY",
    type: "Full-time",
    experienceLevel: "Junior",
    description: "Entry-level position for a Python developer interested in data science and machine learning. Training provided for the right candidate.",
    minSalary: 70000,
    maxSalary: 85000,
    requiredSkills: ["Python", "pandas", "scikit-learn", "SQL", "Git"],
    postedDate: "2024-10-25T00:00:00Z"
  },
  {
    id: 4,
    title: "Frontend Engineer - TypeScript",
    company: "Enterprise Solutions Ltd",
    location: "Austin, TX",
    type: "Contract",
    experienceLevel: "Mid-Level",
    description: "Contract position for an experienced frontend developer with strong TypeScript skills. Work on large-scale enterprise applications.",
    minSalary: 95000,
    maxSalary: 115000,
    requiredSkills: ["TypeScript", "React", "Angular", "JavaScript", "Jest", "Webpack"],
    postedDate: "2024-10-22T00:00:00Z"
  }
]
