export type Service = {
  id: number;
  name: string;
  code: string;
};

export type Ticket = {
  id: number;
  number: number;
  serviceId: number;
  serviceName: string;
  status: string;
  createdAt: string;
  peopleAhead: number;
  counterName: string | null;
  code: string;
  serviceCode: string;
};