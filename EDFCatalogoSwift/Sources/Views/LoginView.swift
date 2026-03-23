import SwiftUI

struct LoginView: View {
    @EnvironmentObject var authViewModel: AuthViewModel
    @State private var email: String = ""
    @State private var password: String = ""
    @State private var errorMessage: String?

    var body: some View {
        VStack(spacing: 16) {
            Text("Iniciar sesión")
                .font(.title2).bold()

            TextField("Email", text: $email)
                .textFieldStyle(.roundedBorder)
                .textContentType(.username)

            SecureField("Contraseña", text: $password)
                .textFieldStyle(.roundedBorder)
                .textContentType(.password)

            if let errorMessage {
                Text(errorMessage)
                    .foregroundColor(.red)
                    .font(.footnote)
            }

            Button("Entrar") {
                Task {
                    await authViewModel.signIn(email: email, password: password)
                    // Si tu signIn pudiera fallar en el futuro, maneja estados en el VM y muéstralos aquí.
                }
            }
            .buttonStyle(.borderedProminent)
        }
        .padding()
        .frame(maxWidth: 400)
    }
}
