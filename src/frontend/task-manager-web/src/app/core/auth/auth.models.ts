export interface AuthInput {
  username: string;
  password: string;
}
export interface CurrentUser {
  userId: number;
  username: string;
}
export interface AuthResult extends CurrentUser {
  accessToken: string;
  expiresAt: string;
}
