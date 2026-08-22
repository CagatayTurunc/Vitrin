import { redirect } from "next/navigation";

// /launches/today → /launches (bugünün lansmanları zaten varsayılan)
export default function LaunchesTodayPage() {
  redirect("/launches");
}
