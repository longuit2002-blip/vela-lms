"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createOrganization, listOrganizations, type CreateOrganizationInput } from "./api";

const organizationsKey = ["organizations"] as const;

export function useOrganizations() {
  return useQuery({ queryKey: organizationsKey, queryFn: listOrganizations });
}

export function useCreateOrganization() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateOrganizationInput) => createOrganization(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: organizationsKey }),
  });
}
