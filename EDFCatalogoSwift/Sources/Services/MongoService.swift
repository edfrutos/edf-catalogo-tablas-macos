import Foundation
import MongoSwift
import NIO

actor MongoService: Sendable {
    static let shared = MongoService()

    private var client: MongoClient?
    private var db: MongoDatabase?
    private var group: EventLoopGroup?

    private init() {}

    /// Entorno del proceso + ficheros `.env` (cwd y bundle Resources), misma semántica que `S3Service`.
    /// Acepta `MONGO_URI` / `MONGO_DB` (plantilla `.env.example`) y `MONGODB_URI` / `MONGODB_DB`.
    private static func resolvedEnvironment() -> [String: String] {
        var env = ProcessInfo.processInfo.environment

        func mergeDotEnv(path: String) {
            guard let text = try? String(contentsOfFile: path, encoding: .utf8) else { return }
            for raw in text.split(separator: "\n", omittingEmptySubsequences: false) {
                let line = raw.trimmingCharacters(in: .whitespaces)
                guard !line.isEmpty, !line.hasPrefix("#"), let eq = line.firstIndex(of: "=") else { continue }
                let k = String(line[..<eq]).trimmingCharacters(in: .whitespaces)
                let v = String(line[line.index(after: eq)...]).trimmingCharacters(in: .whitespaces)
                if !k.isEmpty { env[k] = v }
            }
        }

        mergeDotEnv(path: "\(FileManager.default.currentDirectoryPath)/.env")
        if let res = Bundle.main.resourcePath {
            mergeDotEnv(path: "\(res)/.env")
        }

        return env
    }

    // Conectar si hace falta
    private func connectIfNeeded() async throws {
        if client != nil { return }

        let env = Self.resolvedEnvironment()
        let uri = env["MONGO_URI"] ?? env["MONGODB_URI"] ?? "mongodb://localhost:27017"
        let dbName = env["MONGO_DB"] ?? env["MONGODB_DB"] ?? "edf_catalogo_tablas"

        let g = MultiThreadedEventLoopGroup(numberOfThreads: 1)
        self.group = g
        self.client = try MongoClient(uri, using: g)
        self.db = client!.db(dbName)
    }

    // Cerrar conexión sin bloquear hilo en async
    func disconnect() async {
        if let g = group {
            await withCheckedContinuation { cont in
                g.shutdownGracefully { _ in cont.resume() }
            }
        }
        group = nil
        client = nil
        db = nil
    }

    // MARK: - Helpers visibles desde extensiones del mismo módulo

    func database() async throws -> MongoDatabase {
        try await connectIfNeeded()
        guard let db else {
            throw NSError(domain: "MongoService", code: 1001, userInfo: [NSLocalizedDescriptionKey: "DB no inicializada"])
        }
        return db
    }

    func catalogsCollection() async throws -> MongoCollection<BSONDocument> {
        let db = try await database()
        return db.collection("catalogs")
    }

    func usersCollection() async throws -> MongoCollection<BSONDocument> {
        let db = try await database()
        return db.collection("users")
    }
}
