export enum Permission {
  User,
  Moderator,
  Admin,
}

export class SessionModel {
  id: string;
  name: string;
  permisson: Permission;
}
