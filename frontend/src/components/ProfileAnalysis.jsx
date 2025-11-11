import React from 'react'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
  ArcElement,
  PointElement,
  LineElement,
} from 'chart.js'
import { Bar, Doughnut, Line } from 'react-chartjs-2'
import { Github, Calendar, GitBranch, Star, Users, MapPin, Link } from 'lucide-react'
import LoadingSpinner from './LoadingSpinner'

ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
  ArcElement,
  PointElement,
  LineElement
)

const ProfileAnalysis = ({ profileData, aiAnalysis, isLoading }) => {
  if (isLoading) {
    return (
      <div className="flex justify-center items-center py-16">
        <div className="text-center">
          <LoadingSpinner size="lg" />
          <p className="mt-4 text-gray-600">Analyzing GitHub profile...</p>
        </div>
      </div>
    )
  }

  if (!profileData) {
    return (
      <div className="text-center py-16">
        <p className="text-gray-600">No profile data available</p>
      </div>
    )
  }

  const { profile, repositories, languageStats } = profileData

  // Prepare language data for chart
  const languageData = languageStats?.languages ? Object.entries(languageStats.languages)
    .sort(([,a], [,b]) => b - a)
    .slice(0, 8) : []

  const languageChartData = {
    labels: languageData.map(([lang]) => lang),
    datasets: [
      {
        data: languageData.map(([,count]) => count),
        backgroundColor: [
          '#3B82F6', '#10B981', '#F59E0B', '#EF4444',
          '#8B5CF6', '#06B6D4', '#84CC16', '#F97316'
        ],
        borderWidth: 2,
        borderColor: '#ffffff'
      }
    ]
  }

  // Prepare activity data for chart (mock data since GitHub API doesn't provide this easily)
  const activityData = [] // We'll remove this chart since we don't have this data
  const activityChartData = {
    labels: [],
    datasets: []
  }

  // Repository size distribution
  const repoSizes = repositories?.map(repo => repo.size || 0) || []
  const sizeRanges = ['< 1MB', '1-10MB', '10-50MB', '50MB+']
  const sizeCounts = [
    repoSizes.filter(size => size < 1000).length,
    repoSizes.filter(size => size >= 1000 && size < 10000).length,
    repoSizes.filter(size => size >= 10000 && size < 50000).length,
    repoSizes.filter(size => size >= 50000).length,
  ]

  const repoSizeChartData = {
    labels: sizeRanges,
    datasets: [
      {
        label: 'Repository Count',
        data: sizeCounts,
        backgroundColor: 'rgba(59, 130, 246, 0.6)',
        borderColor: '#3B82F6',
        borderWidth: 2
      }
    ]
  }

  const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom',
        labels: {
          padding: 20,
          usePointStyle: true
        }
      }
    }
  }

  const formatDate = (dateString) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    })
  }

  const formatNumber = (num) => {
    if (num >= 1000) {
      return (num / 1000).toFixed(1) + 'k'
    }
    return num?.toString() || '0'
  }

  return (
    <div className="space-y-8">
      {/* Profile Header */}
      <div className="bg-white rounded-lg shadow-sm border p-6">
        <div className="flex flex-col sm:flex-row items-start sm:items-center space-y-4 sm:space-y-0 sm:space-x-6">
          <img
            src={profile?.avatarUrl}
            alt={profile?.name || profile?.login}
            className="w-20 h-20 rounded-full border-2 border-gray-200"
          />
          <div className="flex-1">
            <h2 className="text-2xl font-bold text-gray-900">
              {profile?.name || profile?.login}
            </h2>
            {profile?.login && profile?.name && (
              <p className="text-gray-600">@{profile.login}</p>
            )}
            {profile?.bio && (
              <p className="text-gray-700 mt-2">{profile.bio}</p>
            )}
            <div className="flex flex-wrap items-center gap-4 mt-3 text-sm text-gray-600">
              {profile?.location && (
                <div className="flex items-center">
                  <MapPin className="h-4 w-4 mr-1" />
                  {profile.location}
                </div>
              )}
              {profile?.blog && (
                <div className="flex items-center">
                  <Link className="h-4 w-4 mr-1" />
                  <a href={profile.blog} target="_blank" rel="noopener noreferrer" 
                     className="text-blue-600 hover:underline">
                    Website
                  </a>
                </div>
              )}
              <div className="flex items-center">
                <Calendar className="h-4 w-4 mr-1" />
                Joined {formatDate(profile?.createdAt)}
              </div>
            </div>
          </div>
          <a
            href={profile?.htmlUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center px-4 py-2 bg-gray-900 text-white rounded-lg hover:bg-gray-800 transition-colors"
          >
            <Github className="h-4 w-4 mr-2" />
            View on GitHub
          </a>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow-sm border p-6 text-center">
          <div className="text-2xl font-bold text-blue-600">{formatNumber(profile?.publicRepos || 0)}</div>
          <div className="text-sm text-gray-600">Repositories</div>
        </div>
        <div className="bg-white rounded-lg shadow-sm border p-6 text-center">
          <div className="text-2xl font-bold text-green-600">{formatNumber(profile?.followers || 0)}</div>
          <div className="text-sm text-gray-600">Followers</div>
        </div>
        <div className="bg-white rounded-lg shadow-sm border p-6 text-center">
          <div className="text-2xl font-bold text-purple-600">{formatNumber(profile?.following || 0)}</div>
          <div className="text-sm text-gray-600">Following</div>
        </div>
        <div className="bg-white rounded-lg shadow-sm border p-6 text-center">
          <div className="text-2xl font-bold text-orange-600">{formatNumber(repositories?.reduce((sum, repo) => sum + (repo.stargazersCount || 0), 0) || 0)}</div>
          <div className="text-sm text-gray-600">Total Stars</div>
        </div>
      </div>

      {/* AI Analysis */}
      {aiAnalysis && (
        <div className="bg-white rounded-lg shadow-sm border p-6">
          <h3 className="text-xl font-semibold text-gray-900 mb-4">🤖 AI Analysis</h3>
          <div className="grid md:grid-cols-2 gap-6">
            <div>
              <h4 className="font-medium text-gray-900 mb-2">Summary</h4>
              <p className="text-gray-700 text-sm leading-relaxed">{aiAnalysis.summary}</p>
            </div>
            <div>
              <h4 className="font-medium text-gray-900 mb-2">Experience Level</h4>
              <div className="flex items-center">
                <span className={`inline-block px-3 py-1 rounded-full text-sm font-medium ${
                  aiAnalysis.experienceLevel === 'Senior' ? 'bg-green-100 text-green-800' :
                  aiAnalysis.experienceLevel === 'Mid-Level' ? 'bg-yellow-100 text-yellow-800' :
                  'bg-blue-100 text-blue-800'
                }`}>
                  {aiAnalysis.experienceLevel}
                </span>
              </div>
            </div>
          </div>
          
          {aiAnalysis.skills && aiAnalysis.skills.length > 0 && (
            <div className="mt-6">
              <h4 className="font-medium text-gray-900 mb-3">Identified Skills</h4>
              <div className="flex flex-wrap gap-2">
                {aiAnalysis.skills.map((skill, index) => (
                  <span key={index} className="px-3 py-1 bg-blue-50 text-blue-700 rounded-full text-sm">
                    {skill}
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Charts */}
      <div className="grid md:grid-cols-2 gap-8">
        {/* Language Distribution */}
        {languageData.length > 0 && (
          <div className="bg-white rounded-lg shadow-sm border p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Programming Languages</h3>
            <div className="h-64">
              <Doughnut data={languageChartData} options={chartOptions} />
            </div>
          </div>
        )}

        {/* Repository Size Distribution */}
        {repositories && repositories.length > 0 && (
          <div className="bg-white rounded-lg shadow-sm border p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Repository Sizes</h3>
            <div className="h-64">
              <Bar data={repoSizeChartData} options={chartOptions} />
            </div>
          </div>
        )}
      </div>

      {/* Activity Chart */}
      {false && activityData.length > 0 && (
        <div className="bg-white rounded-lg shadow-sm border p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Commit Activity (Last 52 Weeks)</h3>
          <div className="h-64">
            <Line data={activityChartData} options={{
              ...chartOptions,
              scales: {
                y: {
                  beginAtZero: true,
                  grid: {
                    color: '#F3F4F6'
                  }
                },
                x: {
                  grid: {
                    color: '#F3F4F6'
                  }
                }
              }
            }} />
          </div>
        </div>
      )}

      {/* Recent Repositories */}
      {repositories && repositories.length > 0 && (
        <div className="bg-white rounded-lg shadow-sm border p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Recent Repositories</h3>
          <div className="grid gap-4">
            {repositories.slice(0, 6).map((repo, index) => (
              <div key={repo.name || index} className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50 transition-colors">
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <h4 className="font-medium text-gray-900">
                      <a href={repo.htmlUrl} target="_blank" rel="noopener noreferrer" 
                         className="hover:text-blue-600 transition-colors">
                        {repo.name}
                      </a>
                    </h4>
                    {repo.description && (
                      <p className="text-sm text-gray-600 mt-1">{repo.description}</p>
                    )}
                    <div className="flex items-center gap-4 mt-2 text-xs text-gray-500">
                      {repo.language && (
                        <span className="flex items-center">
                          <span className="w-3 h-3 rounded-full bg-blue-400 mr-1"></span>
                          {repo.language}
                        </span>
                      )}
                      <span className="flex items-center">
                        <Star className="h-3 w-3 mr-1" />
                        {repo.stargazersCount || 0}
                      </span>
                      <span className="flex items-center">
                        <GitBranch className="h-3 w-3 mr-1" />
                        {repo.forksCount || 0}
                      </span>
                      <span>Updated {new Date(repo.updatedAt).toLocaleDateString()}</span>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

export default ProfileAnalysis
