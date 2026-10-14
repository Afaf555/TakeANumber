export type User = {
  id: number;
  username: string;
  role: string;
  token: string;
};

export type LoginCreds = {
  username: string;
  password: string;
};

export type RegisterCreds = {
  username: string;
  password: string;
};