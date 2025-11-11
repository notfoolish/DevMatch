import React, { useState, useMemo } from 'react'
import { Briefcase, MapPin, DollarSign, Clock, Building2, Filter, Star, AlertTriangle } from 'lucide-react'

const JobMatches = ({ jobs = [], aiAnalysis, profileData }) => {
  const [sortBy, setSortBy] = useState('relevance')
  const [filterLevel, setFilterLevel] = useState('all')
  const [filterType, setFilterType] = useState('all')

  // Calculate job match scores based on AI analysis
  const jobsWithScores = useMemo(() => {
    if (!aiAnalysis || !jobs.length) return jobs

    const userSkills = aiAnalysis.skills?.map(skill => skill.toLowerCase()) || []
    const userLevel = aiAnalysis.experienceLevel?.toLowerCase() || ''

    return jobs.map(job => {
      let score = 0
      let matchingSkills = []
      let missingSkills = []

      // Calculate skill match
      const jobSkills = job.requiredSkills?.map(skill => skill.toLowerCase()) || []
      
      jobSkills.forEach(skill => {
        if (userSkills.some(userSkill => 
          userSkill.includes(skill) || skill.includes(userSkill)
        )) {
          score += 10
          matchingSkills.push(skill)
        } else {
          missingSkills.push(skill)
        }
      })

      // Calculate experience level match
      const jobLevel = job.experienceLevel?.toLowerCase() || ''
      if (jobLevel.includes(userLevel) || userLevel.includes(jobLevel)) {
        score += 20
      } else if (
        (userLevel.includes('senior') && jobLevel.includes('mid')) ||
        (userLevel.includes('mid') && jobLevel.includes('junior'))
      ) {
        score += 10 // Overqualified but still a match
      }

      // Bonus for high-priority skills
      const prioritySkills = ['react', 'javascript', 'typescript', 'python', 'java', 'c#', 'node']
      prioritySkills.forEach(skill => {
        if (matchingSkills.includes(skill)) {
          score += 5
        }
      })

      // Normalize score to 0-100
      const maxPossibleScore = (jobSkills.length * 10) + 20 + (prioritySkills.length * 5)
      const normalizedScore = maxPossibleScore > 0 ? Math.min(100, (score / maxPossibleScore) * 100) : 0

      return {
        ...job,
        matchScore: Math.round(normalizedScore),
        matchingSkills,
        missingSkills: missingSkills.slice(0, 5) // Limit to top 5 missing skills
      }
    })
  }, [jobs, aiAnalysis])

  // Filter and sort jobs
  const filteredJobs = useMemo(() => {
    let filtered = jobsWithScores

    // Filter by experience level
    if (filterLevel !== 'all') {
      filtered = filtered.filter(job => 
        job.experienceLevel?.toLowerCase().includes(filterLevel.toLowerCase())
      )
    }

    // Filter by job type
    if (filterType !== 'all') {
      filtered = filtered.filter(job => 
        job.type?.toLowerCase() === filterType.toLowerCase()
      )
    }

    // Sort jobs
    return filtered.sort((a, b) => {
      switch (sortBy) {
        case 'relevance':
          return (b.matchScore || 0) - (a.matchScore || 0)
        case 'salary':
          return (b.maxSalary || 0) - (a.maxSalary || 0)
        case 'recent':
          return new Date(b.postedDate) - new Date(a.postedDate)
        default:
          return 0
      }
    })
  }, [jobsWithScores, sortBy, filterLevel, filterType])

  const formatSalary = (min, max) => {
    if (!min && !max) return 'Salary not specified'
    if (!max) return `$${min?.toLocaleString()}+`
    if (!min) return `Up to $${max?.toLocaleString()}`
    return `$${min?.toLocaleString()} - $${max?.toLocaleString()}`
  }

  const formatDate = (dateString) => {
    const date = new Date(dateString)
    const now = new Date()
    const diffTime = Math.abs(now - date)
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24))
    
    if (diffDays === 1) return 'Yesterday'
    if (diffDays < 7) return `${diffDays} days ago`
    if (diffDays < 30) return `${Math.ceil(diffDays / 7)} weeks ago`
    return `${Math.ceil(diffDays / 30)} months ago`
  }

  const getMatchColor = (score) => {
    if (score >= 80) return 'text-green-600 bg-green-50 border-green-200'
    if (score >= 60) return 'text-yellow-600 bg-yellow-50 border-yellow-200'
    if (score >= 40) return 'text-orange-600 bg-orange-50 border-orange-200'
    return 'text-red-600 bg-red-50 border-red-200'
  }

  const getMatchIcon = (score) => {
    if (score >= 80) return <Star className="h-4 w-4" />
    if (score >= 40) return <AlertTriangle className="h-4 w-4" />
    return null
  }

  return (
    <div className="space-y-6">
      {/* Header with filters */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Job Matches</h2>
          <p className="text-gray-600">
            {filteredJobs.length} job{filteredJobs.length !== 1 ? 's' : ''} found
            {aiAnalysis && ' • Sorted by AI match score'}
          </p>
        </div>
        
        <div className="flex flex-wrap gap-2">
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            <option value="relevance">Sort by Relevance</option>
            <option value="salary">Sort by Salary</option>
            <option value="recent">Sort by Date</option>
          </select>
          
          <select
            value={filterLevel}
            onChange={(e) => setFilterLevel(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            <option value="all">All Levels</option>
            <option value="junior">Junior</option>
            <option value="mid">Mid-Level</option>
            <option value="senior">Senior</option>
          </select>
          
          <select
            value={filterType}
            onChange={(e) => setFilterType(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            <option value="all">All Types</option>
            <option value="fulltime">Full-time</option>
            <option value="contract">Contract</option>
            <option value="remote">Remote</option>
          </select>
        </div>
      </div>

      {/* AI Analysis Summary */}
      {aiAnalysis && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <h3 className="font-medium text-blue-900 mb-2">🎯 Your Profile Summary</h3>
          <div className="grid sm:grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-blue-700 font-medium">Experience Level: </span>
              <span className="text-blue-800">{aiAnalysis.experienceLevel}</span>
            </div>
            {aiAnalysis.skills && (
              <div>
                <span className="text-blue-700 font-medium">Top Skills: </span>
                <span className="text-blue-800">{aiAnalysis.skills.slice(0, 3).join(', ')}</span>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Job List */}
      {filteredJobs.length === 0 ? (
        <div className="text-center py-16">
          <Briefcase className="h-16 w-16 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">No jobs found</h3>
          <p className="text-gray-600">
            Try adjusting your filters or check back later for new opportunities.
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {filteredJobs.map((job) => (
            <div
              key={job.id}
              className="bg-white border border-gray-200 rounded-lg p-6 hover:border-blue-300 hover:shadow-md transition-all"
            >
              <div className="flex justify-between items-start mb-4">
                <div className="flex-1">
                  <div className="flex items-start justify-between mb-2">
                    <h3 className="text-xl font-semibold text-gray-900 mr-4">
                      {job.title}
                    </h3>
                    {aiAnalysis && typeof job.matchScore === 'number' && (
                      <div className={`flex items-center px-3 py-1 rounded-full border text-sm font-medium ${getMatchColor(job.matchScore)}`}>
                        {getMatchIcon(job.matchScore)}
                        <span className="ml-1">{job.matchScore}% Match</span>
                      </div>
                    )}
                  </div>
                  
                  <div className="flex items-center text-gray-600 mb-2">
                    <Building2 className="h-4 w-4 mr-2" />
                    <span className="font-medium">{job.company}</span>
                    {job.location && (
                      <>
                        <span className="mx-2">•</span>
                        <MapPin className="h-4 w-4 mr-1" />
                        <span>{job.location}</span>
                      </>
                    )}
                  </div>

                  <div className="flex flex-wrap items-center gap-4 text-sm text-gray-600 mb-3">
                    {job.type && (
                      <span className="px-2 py-1 bg-gray-100 rounded text-gray-700">
                        {job.type}
                      </span>
                    )}
                    {job.experienceLevel && (
                      <span className="px-2 py-1 bg-gray-100 rounded text-gray-700">
                        {job.experienceLevel}
                      </span>
                    )}
                    <div className="flex items-center">
                      <DollarSign className="h-4 w-4 mr-1" />
                      {formatSalary(job.minSalary, job.maxSalary)}
                    </div>
                    <div className="flex items-center">
                      <Clock className="h-4 w-4 mr-1" />
                      {formatDate(job.postedDate)}
                    </div>
                  </div>

                  {job.description && (
                    <p className="text-gray-700 mb-4 line-clamp-2">
                      {job.description}
                    </p>
                  )}
                </div>
              </div>

              {/* Skills and Match Details */}
              {aiAnalysis && (job.matchingSkills?.length > 0 || job.missingSkills?.length > 0) && (
                <div className="border-t border-gray-200 pt-4">
                  <div className="grid sm:grid-cols-2 gap-4">
                    {job.matchingSkills?.length > 0 && (
                      <div>
                        <h4 className="text-sm font-medium text-green-700 mb-2">
                          ✅ Your Matching Skills
                        </h4>
                        <div className="flex flex-wrap gap-1">
                          {job.matchingSkills.map((skill, index) => (
                            <span
                              key={index}
                              className="px-2 py-1 bg-green-50 text-green-700 border border-green-200 rounded text-xs"
                            >
                              {skill}
                            </span>
                          ))}
                        </div>
                      </div>
                    )}
                    
                    {job.missingSkills?.length > 0 && (
                      <div>
                        <h4 className="text-sm font-medium text-orange-700 mb-2">
                          📚 Skills to Learn
                        </h4>
                        <div className="flex flex-wrap gap-1">
                          {job.missingSkills.map((skill, index) => (
                            <span
                              key={index}
                              className="px-2 py-1 bg-orange-50 text-orange-700 border border-orange-200 rounded text-xs"
                            >
                              {skill}
                            </span>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* Required Skills (fallback if no AI analysis) */}
              {!aiAnalysis && job.requiredSkills?.length > 0 && (
                <div className="border-t border-gray-200 pt-4">
                  <h4 className="text-sm font-medium text-gray-700 mb-2">Required Skills</h4>
                  <div className="flex flex-wrap gap-2">
                    {job.requiredSkills.map((skill, index) => (
                      <span
                        key={index}
                        className="px-2 py-1 bg-gray-100 text-gray-700 rounded text-sm"
                      >
                        {skill}
                      </span>
                    ))}
                  </div>
                </div>
              )}

              {/* Apply Button */}
              <div className="mt-4 pt-4 border-t border-gray-200">
                <button className="w-full sm:w-auto px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors">
                  View Details & Apply
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export default JobMatches
