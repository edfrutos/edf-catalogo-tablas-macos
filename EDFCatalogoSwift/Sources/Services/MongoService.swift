import Foundation
import MongoSwift
import NIO

actor MongoService: Sendable {
    static let shared = MongoService()

    private var client: MongoClient?
    private var db: MongoDatabase?
    private var group: EventLoopGroup?

    private init() {}

    // Conectar si hace falta
    private func connectIfNeeded() async throws {
        if client != nil { return }

        let uri = ProcessInfo.processInfo.environment["MONGODB_URI"] ?? "mongodb://localhost:27017"
        let dbName = ProcessInfo.processInfo.environment["MONGODB_DB"] ?? "edf_catalogo_tablas"

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
