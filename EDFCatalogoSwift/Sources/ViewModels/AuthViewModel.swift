import Foundation

@MainActor
final class AuthViewModel: ObservableObject {
    @Published var isAuthenticated: Bool = false
    @Published var currentUser: User?
    @Published var isLoading: Bool = false
    @Published var errorMessage: String?

    private let mongo = MongoService.shared

    /// Login principal. Mantiene la firma (email, password) para no romper llamadas existentes.
    /// Nota: ahora está aislado al MainActor (la clase entera), así que no hace falta `Task { }` ni `MainActor.run`.
    func signIn(email: String, password: String) async {
        isLoading = true
        errorMessage = nil
        defer { isLoading = false }

        do {
            let exists = try await mongo.checkUserExists(email: email)
            if exists {
                // Sustituye por el usuario real recuperado de tu backend cuando lo tengas
                currentUser = User.mock(email: email)
                isAuthenticated = true
            } else {
                currentUser = nil
                isAuthenticated = false
                errorMessage = "Credenciales no válidas."
            }
        } catch {
            currentUser = nil
            isAuthenticated = false
            errorMessage = error.localizedDescription
        }
    }

    /// Cierre de sesión. No es `async` ni usa `Task` porque la clase está aislada al MainActor.
    func signOut() {
        currentUser = nil
        isAuthenticated = false
        errorMessage = nil
    }
}
