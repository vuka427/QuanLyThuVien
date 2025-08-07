export interface Member {
  memberId?: number;
  memberCode: string;
  fullName: string;
  email?: string;
  phone?: string;
  address?: string;
  dateOfBirth: string;
  membershipDate: string;
  isActive: boolean;
}