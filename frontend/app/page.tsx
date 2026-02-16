export default function Home() {
  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
          <h1 className="text-2xl font-bold text-gray-900">Book Rating Platform</h1>
        </div>
      </nav>
      
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="text-center">
          <h2 className="text-4xl font-bold text-gray-900 mb-4">
            Discover Your Next Great Read
          </h2>
          <p className="text-xl text-gray-600 mb-8">
            Rate, review, and track your reading journey
          </p>
          <div className="flex gap-4 justify-center">
            <button className="bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 transition">
              Browse Books
            </button>
            <button className="bg-gray-200 text-gray-900 px-6 py-3 rounded-lg hover:bg-gray-300 transition">
              Sign In
            </button>
          </div>
        </div>
      </main>
    </div>
  );
}
