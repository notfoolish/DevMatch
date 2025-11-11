import { useState, useEffect } from 'react'
import { Search, Github, Briefcase, BarChart3, Users, AlertCircle, CheckCircle } from 'lucide-react'
import axios from 'axios'
import ProfileAnalysis from './components/ProfileAnalysis'
import JobMatches from './components/JobMatches'
import LoadingSpinner from './components/LoadingSpinner'

// Configure axios base URL
const API_BASE_URL = 'http://localhost:5000'
axios.defaults.baseURL = API_BASE_URL

function App() {
  const [githubUsername, setGithubUsername] = useState('')
  const [isAnalyzing, setIsAnalyzing] = useState(false)
  const [profileData, setProfileData] = useState(null)
  const [aiAnalysis, setAiAnalysis] = useState(null)
  const [jobMatches, setJobMatches] = useState([])
  const [error, setError] = useState('')
  const [activeTab, setActiveTab] = useState('analysis')

  // State management explanation:
  // useState manages component-level state for form inputs, loading states, and data
  // useEffect handles side effects like API calls and data synchronization
  // When githubUsername changes, we reset previous data to avoid stale state
  // The loading state (isAnalyzing) prevents multiple simultaneous requests

  const handleAnalyze = async () => {
    if (!githubUsername.trim()) return
    
    setIsAnalyzing(true)
    setError('')
    setProfileData(null)
    setAiAnalysis(null)
    setJobMatches([])
    
    try {
      // Fetch GitHub profile data
      console.log(`Fetching profile data for: ${githubUsername}`)
      const profileResponse = await axios.get(`/api/github/${githubUsername}`)
      setProfileData(profileResponse.data)
      
      // Fetch AI analysis
      console.log(`Fetching AI analysis for: ${githubUsername}`)
      const aiResponse = await axios.get(`/api/ai/analyze/${githubUsername}`)
      setAiAnalysis(aiResponse.data)
      
      // Fetch available jobs (we'll match them client-side with AI data)
      console.log('Fetching available jobs...')
      const jobsResponse = await axios.get('/api/jobs')
      setJobMatches(jobsResponse.data || [])
      
      // Switch to analysis tab after successful fetch
      setActiveTab('analysis')
      
    } catch (err) {
      console.error('Error analyzing profile:', err)
      let errorMessage = 'Failed to analyze GitHub profile. Please check the username and try again.'
      
      if (err.code === 'ERR_NETWORK' || err.message.includes('Network Error')) {
        errorMessage = 'Unable to connect to the backend server. Make sure the ASP.NET Core backend is running on http://localhost:5000'
      } else if (err.response?.status === 404) {
        errorMessage = 'GitHub user not found. Please check the username and try again.'
      } else if (err.response?.data?.message) {
        errorMessage = err.response.data.message
      }
      
      setError(errorMessage)
    } finally {
      setIsAnalyzing(false)
    }
  }

  // Reset data when username changes
  useEffect(() => {
    if (profileData && githubUsername !== profileData.profile?.login) {
      setProfileData(null)
      setAiAnalysis(null)
      setJobMatches([])
      setError('')
    }
  }, [githubUsername, profileData])

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
      {/* Header */}
      <header className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center">
              <Github className="h-8 w-8 text-blue-600 mr-3" />
              <h1 className="text-2xl font-bold text-gray-900">DevMatch</h1>
            </div>
            <nav className="hidden md:flex space-x-8">
              <button 
                onClick={() => setActiveTab('analysis')} 
                className={`transition-colors ${activeTab === 'analysis' ? 'text-blue-600' : 'text-gray-700 hover:text-blue-600'}`}
              >
                Analysis
              </button>
              <button 
                onClick={() => setActiveTab('jobs')} 
                className={`transition-colors ${activeTab === 'jobs' ? 'text-blue-600' : 'text-gray-700 hover:text-blue-600'}`}
              >
                Jobs
              </button>
            </nav>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <section className="py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-4xl mx-auto text-center">
          <h2 className="text-4xl md:text-5xl font-bold text-gray-900 mb-6">
            AI-Powered GitHub Profile Analysis
          </h2>
          <p className="text-xl text-gray-600 mb-8 max-w-2xl mx-auto">
            Analyze your GitHub profile with AI and get matched with perfect job opportunities
            based on your skills, experience, and coding patterns.
          </p>
          
          {/* Search Input */}
          <div className="max-w-md mx-auto mb-8">
            <div className="relative">
              <input
                type="text"
                value={githubUsername}
                onChange={(e) => setGithubUsername(e.target.value)}
                placeholder="Enter GitHub username..."
                className="w-full px-4 py-3 pl-12 pr-4 text-gray-700 bg-white border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none transition-all"
                onKeyPress={(e) => e.key === 'Enter' && handleAnalyze()}
              />
              <Search className="absolute left-4 top-3.5 h-5 w-5 text-gray-400" />
            </div>
            <button
              onClick={handleAnalyze}
              disabled={!githubUsername.trim() || isAnalyzing}
              className="w-full mt-4 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 text-white font-semibold py-3 px-6 rounded-lg transition-colors"
            >
              {isAnalyzing ? (
                <div className="flex items-center justify-center">
                  <LoadingSpinner size="sm" />
                  <span className="ml-2">Analyzing...</span>
                </div>
              ) : 'Analyze Profile'}
            </button>
          </div>

          {/* Error Message */}
          {error && (
            <div className="max-w-md mx-auto mb-8 p-4 bg-red-50 border border-red-200 rounded-lg flex items-start">
              <AlertCircle className="h-5 w-5 text-red-500 mt-0.5 mr-3 flex-shrink-0" />
              <p className="text-red-700 text-sm">{error}</p>
            </div>
          )}

          {/* Success Message */}
          {profileData && !isAnalyzing && (
            <div className="max-w-md mx-auto mb-8 p-4 bg-green-50 border border-green-200 rounded-lg flex items-center">
              <CheckCircle className="h-5 w-5 text-green-500 mr-3" />
              <p className="text-green-700 text-sm">Profile analysis complete!</p>
            </div>
          )}
        </div>
      </section>

      {/* Analysis Results */}
      {profileData && (
        <section className="py-8 px-4 sm:px-6 lg:px-8">
          <div className="max-w-7xl mx-auto">
            {/* Tab Navigation */}
            <div className="border-b border-gray-200 mb-8">
              <div className="flex space-x-8">
                <button
                  onClick={() => setActiveTab('analysis')}
                  className={`py-2 px-1 border-b-2 font-medium text-sm transition-colors ${
                    activeTab === 'analysis'
                      ? 'border-blue-500 text-blue-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <BarChart3 className="inline h-4 w-4 mr-2" />
                  Profile Analysis
                </button>
                <button
                  onClick={() => setActiveTab('jobs')}
                  className={`py-2 px-1 border-b-2 font-medium text-sm transition-colors ${
                    activeTab === 'jobs'
                      ? 'border-blue-500 text-blue-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <Briefcase className="inline h-4 w-4 mr-2" />
                  Job Matches ({jobMatches.length})
                </button>
              </div>
            </div>

            {/* Tab Content */}
            {activeTab === 'analysis' && (
              <ProfileAnalysis 
                profileData={profileData} 
                aiAnalysis={aiAnalysis}
                isLoading={isAnalyzing}
              />
            )}

            {activeTab === 'jobs' && (
              <JobMatches 
                jobs={jobMatches}
                aiAnalysis={aiAnalysis}
                profileData={profileData}
              />
            )}
          </div>
        </section>
      )}

      {/* Features Section - Only show if no data */}
      {!profileData && !isAnalyzing && (
        <section className="py-16 px-4 sm:px-6 lg:px-8 bg-white">
          <div className="max-w-6xl mx-auto">
            <h3 className="text-3xl font-bold text-center text-gray-900 mb-12">
              How DevMatch Works
            </h3>
            <div className="grid md:grid-cols-3 gap-8">
              <div className="text-center p-6">
                <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
                  <Github className="h-8 w-8 text-blue-600" />
                </div>
                <h4 className="text-xl font-semibold text-gray-900 mb-2">
                  Analyze GitHub Profile
                </h4>
                <p className="text-gray-600">
                  Our AI analyzes your repositories, commit patterns, and coding style
                  to understand your skills and experience level.
                </p>
              </div>
              
              <div className="text-center p-6">
                <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
                  <BarChart3 className="h-8 w-8 text-green-600" />
                </div>
                <h4 className="text-xl font-semibold text-gray-900 mb-2">
                  Generate Insights
                </h4>
                <p className="text-gray-600">
                  Get detailed insights about your tech stack, experience level,
                  and areas of expertise with visual analytics.
                </p>
              </div>
              
              <div className="text-center p-6">
                <div className="w-16 h-16 bg-purple-100 rounded-full flex items-center justify-center mx-auto mb-4">
                  <Users className="h-8 w-8 text-purple-600" />
                </div>
                <h4 className="text-xl font-semibold text-gray-900 mb-2">
                  Find Perfect Matches
                </h4>
                <p className="text-gray-600">
                  Get matched with job opportunities that align with your skills,
                  experience, and career aspirations.
                </p>
              </div>
            </div>
          </div>
        </section>
      )}

      {/* Footer */}
      <footer className="bg-gray-800 text-white py-8">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <p className="text-gray-400">
            © 2025 DevMatch. Built with React, Tailwind CSS, and ASP.NET Core.
          </p>
        </div>
      </footer>
    </div>
  )
}

export default App
