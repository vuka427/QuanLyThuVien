import { spec } from "node:test/reporters";

export interface User {
  id: number;
  userName: string;
  fullName: string;
  email: string;
  role: string[];
}